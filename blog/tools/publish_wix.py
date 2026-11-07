#!/usr/bin/env python3
"""Publish scheduled Markdown blog posts to a Wix blog.

Each post in blog/posts/*.md starts with a small front-matter block:

    ---
    title: My post title
    publish_at: 2026-11-10T09:00:00-05:00   (ISO 8601 with offset)
    status: ready                           (draft = never published)
    excerpt: One-sentence summary (optional)
    ---

A post is sent to Wix when status is "ready" and publish_at is in the past
and it is not already listed in blog/published.json.

Environment variables:
    WIX_API_KEY    API key with Blog permissions       (required)
    WIX_SITE_ID    Wix site id                         (required)
    WIX_MEMBER_ID  Member id of the post author        (required)
    WIX_MODE       "publish" (default) or "draft"; draft only creates
                   a draft in Wix for you to publish by hand.

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
    for key in ("title", "publish_at"):
        if key not in meta:
            raise ValueError("%s: front matter needs '%s'" % (path, key))
    when = datetime.datetime.fromisoformat(meta["publish_at"])
    if when.tzinfo is None:
        raise ValueError("%s: publish_at needs a UTC offset, e.g. -05:00" % path)
    meta["when"] = when
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
                           "nodes": [_paragraph(i)]} for i in items],
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
    draft_id = created["draftPost"]["id"]
    if mode == "publish":
        wix_post("/draft-posts/%s/publish" % draft_id, {}, env)
    return draft_id


# ---------- main ----------

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    state = {}
    if os.path.exists(STATE_FILE):
        with open(STATE_FILE, encoding="utf-8") as f:
            state = json.load(f)

    now = datetime.datetime.now(datetime.timezone.utc)
    due = []
    for name in sorted(os.listdir(POSTS_DIR)):
        if not name.endswith(".md"):
            continue
        post = parse_post(os.path.join(POSTS_DIR, name))
        if post.get("status") != "ready" or post["id"] in state:
            continue
        if post["when"] <= now:
            due.append(post)

    if not due:
        print("Nothing due.")
        return 0

    mode = os.environ.get("WIX_MODE", "publish")
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
