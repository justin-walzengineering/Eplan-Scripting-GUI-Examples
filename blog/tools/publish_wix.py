#!/usr/bin/env python3
"""Publish scheduled Markdown blog posts to a Wix blog.

Each post in blog/posts/*.md starts with a small front-matter block:

    ---
    title: My post title
    status: ready                           (draft = never published)
    excerpt: One-sentence summary (optional)
    ---

Posts are released ONE AT A TIME, in file-name order, on a fixed weekly
schedule (Tuesday and Friday, 09:xx in BLOG_TZ). On each scheduled slot the
first "ready" post not yet listed in blog/published.json is sent. Name files
01-..., 02-... to set the order.

Environment variables:
    WIX_API_KEY    API key with Blog permissions       (required)
    WIX_SITE_ID    Wix site id                         (required)
    WIX_MEMBER_ID  Member id of the post author        (required)
    BLOG_TZ        IANA time zone for the schedule (default America/New_York)
    WIX_MODE       "draft" (default) creates a draft in Wix for you to
                   publish by hand; "publish" publishes immediately.

Usage:
    python3 blog/tools/publish_wix.py --dry-run   # show what would happen
    python3 blog/tools/publish_wix.py             # do it

Only the Python standard library is used.

NOTE: The Wix endpoints and the Ricos JSON shape below follow Wix's public
Blog REST API as the author understands it. Verify with --dry-run and with
WIX_MODE=draft first, and check Wix's current API docs if a call is rejected.
"""
import argparse
import datetime
import json
import os
import re
import sys
import urllib.error
import urllib.request
import uuid
import zoneinfo

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
POSTS_DIR = os.path.join(ROOT, "posts")
STATE_FILE = os.path.join(ROOT, "published.json")
API = "https://www.wixapis.com/blog/v3"


# ---------- reading posts ----------

def parse_post(path):
    with open(path, encoding="utf-8") as f:
        text = f.read()
    m = re.match(r"^---\s*\n(.*?)\n---\s*\n(.*)$", text, re.S)
    if not m:
        raise ValueError("%s: missing front matter" % path)
    meta = {}
    for line in m.group(1).splitlines():
        if ":" in line:
            k, v = line.split(":", 1)
            meta[k.strip()] = v.strip()
    if "title" not in meta:
        raise ValueError("%s: front matter needs 'title'" % path)
    meta["body"] = m.group(2)
    meta["id"] = os.path.splitext(os.path.basename(path))[0]
    return meta


# ---------- Markdown -> Ricos (small subset) ----------

def _id():
    return str(uuid.uuid4())


def _text_node(text):
    return {"type": "TEXT", "id": _id(), "nodes": [],
            "textData": {"text": text, "decorations": []}}


def _paragraph(text):
    return {"type": "PARAGRAPH", "id": _id(), "nodes": [_text_node(text)],
            "paragraphData": {}}


def markdown_to_ricos(md):
    """Headings (#..###), paragraphs, bullet lists, fenced code blocks."""
    nodes, para, items, code = [], [], [], None

    def flush_para():
        if para:
            nodes.append(_paragraph(" ".join(para)))
            del para[:]

    def flush_items():
        if items:
            nodes.append({
                "type": "BULLETED_LIST", "id": _id(),
                "nodes": [{"type": "LIST_ITEM", "id": _id(),
                           "nodes": [_paragraph(i)],
                           "listItemData": {}} for i in items],
                "bulletedListData": {}})
            del items[:]

    for line in md.splitlines():
        if line.startswith("```"):
            if code is None:
                flush_para(); flush_items(); code = []
            else:
                nodes.append({
                    "type": "CODE_BLOCK", "id": _id(),
                    "nodes": [_text_node("\n".join(code))],
                    "codeBlockData": {}})
                code = None
            continue
        if code is not None:
            code.append(line)
            continue
        h = re.match(r"^(#{1,3})\s+(.*)$", line)
        b = re.match(r"^[-*]\s+(.*)$", line)
        if h:
            flush_para(); flush_items()
            nodes.append({"type": "HEADING", "id": _id(),
                          "nodes": [_text_node(h.group(2))],
                          "headingData": {"level": len(h.group(1))}})
        elif b:
            flush_para(); items.append(b.group(1))
        elif not line.strip():
            flush_para(); flush_items()
        else:
            flush_items(); para.append(line.strip())
    flush_para(); flush_items()
    return {"nodes": nodes}


# ---------- Wix calls ----------

def wix_post(path, payload, env):
    req = urllib.request.Request(
        API + path, data=json.dumps(payload).encode("utf-8"), method="POST",
        headers={"Authorization": env["WIX_API_KEY"],
                 "wix-site-id": env["WIX_SITE_ID"],
                 "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read().decode("utf-8") or "{}")
    except urllib.error.HTTPError as e:
        raise RuntimeError("Wix %s -> HTTP %s: %s"
                           % (path, e.code, e.read().decode("utf-8", "replace")))


def send_to_wix(post, mode, env):
    draft = {"title": post["title"],
             "memberId": env["WIX_MEMBER_ID"],
             "richContent": markdown_to_ricos(post["body"])}
    if post.get("excerpt"):
        draft["excerpt"] = post["excerpt"]
    created = wix_post("/draft-posts", {"draftPost": draft}, env)
    # Response is documented as the created DraftPost; accept it wrapped or bare.
    draft_id = (created.get("draftPost") or created).get("id")
    if not draft_id:
        raise RuntimeError("Unexpected create-draft response: %r" % created)
    if mode == "publish":
        wix_post("/draft-posts/%s/publish" % draft_id, {}, env)
    return draft_id


# ---------- main ----------

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--force", action="store_true",
                    help="ignore the Tuesday/Friday 9 AM check (manual runs)")
    args = ap.parse_args()

    state = {}
    if os.path.exists(STATE_FILE):
        with open(STATE_FILE, encoding="utf-8") as f:
            state = json.load(f)

    now = datetime.datetime.now(datetime.timezone.utc)
    tz = zoneinfo.ZoneInfo(os.environ.get("BLOG_TZ", "America/New_York"))
    local = now.astimezone(tz)
    # weekday(): Monday=0, Tuesday=1, Friday=4
    if not args.force and not (local.weekday() in (1, 4) and local.hour == 9):
        print("Not a release slot (%s). Nothing to do." % local.strftime("%a %H:%M"))
        return 0

    due = []
    for name in sorted(os.listdir(POSTS_DIR)):
        if not name.endswith(".md"):
            continue
        post = parse_post(os.path.join(POSTS_DIR, name))
        if post.get("status") == "ready" and post["id"] not in state:
            due.append(post)
    due = due[:1]  # one post per slot

    if not due:
        print("No ready posts in the queue.")
        return 0

    mode = os.environ.get("WIX_MODE", "draft")
    env = {k: os.environ.get(k, "") for k in
           ("WIX_API_KEY", "WIX_SITE_ID", "WIX_MEMBER_ID")}
    if not args.dry_run and not all(env.values()):
        print("Wix secrets are not set; skipping %d due post(s)." % len(due))
        return 0

    for post in due:
        if args.dry_run:
            print("[dry-run] would %s: %s" % (mode, post["title"]))
            continue
        draft_id = send_to_wix(post, mode, env)
        state[post["id"]] = {"wix_draft_id": draft_id, "mode": mode,
                             "sent_at": now.isoformat()}
        with open(STATE_FILE, "w", encoding="utf-8") as f:
            json.dump(state, f, indent=2, sort_keys=True)
        print("Sent (%s): %s" % (mode, post["title"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
