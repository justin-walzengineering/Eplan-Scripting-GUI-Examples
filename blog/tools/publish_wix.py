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
01-..., 02-... to set the order. Posts with "type: highlight" are released only
on a Friday, and only when no script post is ready (a --force run ignores the
Friday check). With --preview DIR the script writes the review email text for
the next post instead of sending it to Wix.

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
    # Wix shows the title above the post, so drop a leading "# Title" line
    # from the body to avoid showing the title twice.
    meta["body"] = re.sub(r"^\s*#\s+[^\n]*\n", "", m.group(2), count=1)
    meta["id"] = os.path.splitext(os.path.basename(path))[0]
    return meta


# ---------- Markdown -> Ricos (small subset) ----------

def _id():
    return str(uuid.uuid4())


def _text_node(text):
    return {"type": "TEXT", "id": _id(), "nodes": [],
            "textData": {"text": text, "decorations": []}}


_LINK_RE = re.compile(r"\[([^\]]+)\]\((https?://[^)\s]+)\)|(https?://[^\s<>)]+[^\s<>).,;:!?])")


def _link_node(label, url):
    return {"type": "TEXT", "id": _id(), "nodes": [],
            "textData": {"text": label, "decorations": [
                {"type": "LINK", "linkData": {"link": {
                    "url": url, "target": "BLANK",
                    "rel": {"noreferrer": True}}}}]}}


def _inline_nodes(text):
    """Plain text with [label](url) links and bare http(s) URLs made clickable."""
    nodes, pos = [], 0
    for m in _LINK_RE.finditer(text):
        if m.start() > pos:
            nodes.append(_text_node(text[pos:m.start()]))
        if m.group(1):
            nodes.append(_link_node(m.group(1), m.group(2)))
        else:
            nodes.append(_link_node(m.group(3), m.group(3)))
        pos = m.end()
    if pos < len(text):
        nodes.append(_text_node(text[pos:]))
    return nodes or [_text_node(text)]


def _paragraph(text):
    return {"type": "PARAGRAPH", "id": _id(), "nodes": _inline_nodes(text),
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
                          "nodes": _inline_nodes(h.group(2)),
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

def pick_due(state, local, force):
    """The one post for this slot, or [].

    Script posts (new / update) come first and can use either slot. A community
    highlight is used only when NO script post is ready, and only on a Friday
    slot (a manual --force run ignores the Friday check).
    """
    scripts, highlights = [], []
    for name in sorted(os.listdir(POSTS_DIR)):
        if not name.endswith(".md"):
            continue
        post = parse_post(os.path.join(POSTS_DIR, name))
        post["file"] = name
        if post.get("status") != "ready" or post["id"] in state:
            continue
        (highlights if post.get("type") == "highlight" else scripts).append(post)
    if scripts:
        return scripts[:1]
    if highlights and (force or local.weekday() == 4):
        return highlights[:1]
    return []


def write_preview(post, out_dir, when_text):
    """Write subject.txt and body.txt for the review email."""
    repo = os.environ.get("GITHUB_REPOSITORY", "justin-walzengineering/Eplan-Scripting-GUI-Examples")
    server = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
    edit_url = "%s/%s/edit/main/blog/posts/%s" % (server, repo, post["file"])
    os.makedirs(out_dir, exist_ok=True)
    with open(os.path.join(out_dir, "subject.txt"), "w", encoding="utf-8") as f:
        f.write("Review: \"%s\" goes live %s" % (post["title"], when_text))
    body = (
        "This post is scheduled to go live %s.\n\n"
        "To change it: edit the file on GitHub before then (commit to main):\n%s\n\n"
        "To hold it back: change 'status: ready' to 'status: draft' in the same file.\n"
        "Images and the final look are added/checked in Wix after it is sent.\n\n"
        "------------------------------------------------------------\n"
        "Title: %s\nType: %s\n\n%s\n"
    ) % (when_text, edit_url, post["title"], post.get("type", "new"), post["body"])
    with open(os.path.join(out_dir, "body.txt"), "w", encoding="utf-8") as f:
        f.write(body)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--force", action="store_true",
                    help="ignore the Tuesday/Friday time check (manual runs)")
    ap.add_argument("--preview", metavar="DIR",
                    help="at 8 AM on a slot day, write the review email for the "
                         "next post into DIR instead of sending it to Wix")
    args = ap.parse_args()

    state = {}
    if os.path.exists(STATE_FILE):
        with open(STATE_FILE, encoding="utf-8") as f:
            state = json.load(f)

    now = datetime.datetime.now(datetime.timezone.utc)
    tz = zoneinfo.ZoneInfo(os.environ.get("BLOG_TZ", "America/New_York"))
    local = now.astimezone(tz)
    # weekday(): Monday=0, Tuesday=1, Friday=4
    slot_hour = 8 if args.preview else 9
    if not args.force and not (local.weekday() in (1, 4) and local.hour == slot_hour):
        print("Not a %s slot (%s). Nothing to do."
              % ("preview" if args.preview else "release", local.strftime("%a %H:%M")))
        return 0

    due = pick_due(state, local, args.force)

    if not due:
        print("No ready posts in the queue.")
        return 0

    if args.preview:
        write_preview(due[0], args.preview, "today at 9:00 AM (%s)" % tz.key)
        print("Preview written for: %s" % due[0]["title"])
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
