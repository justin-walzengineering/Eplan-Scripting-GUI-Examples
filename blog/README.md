# Blog posts

Markdown posts that a GitHub Actions workflow sends to the Wix blog on schedule.
This is an unofficial community resource; keep posts educational and original
(see the root `CLAUDE.md` hard rules).

## Writing a post

Copy [TEMPLATE.md](TEMPLATE.md). Use `type: update` plus a "What's new" section when a script you already covered has changed.


Add `blog/posts/NN-short-name.md` (the number sets the order):

```
---
title: Post title
status: draft
excerpt: Optional one-sentence summary
---

Body in Markdown.
```

- `status: draft` is never sent. Change it to `ready` when the post is final.
- Posts are released **one at a time, every Tuesday and Friday at 9 AM**. Each slot sends the
  first `ready` post (lowest file name) that has not been sent. No ready posts means nothing happens.
- The time zone is `America/New_York` unless you add a repository variable `BLOG_TZ`
  (for example `America/Chicago`). Daylight saving is handled.
- To release the next post immediately: Actions tab > Publish scheduled blog posts > Run workflow > tick "force".
- Supported Markdown: `#`/`##`/`###` headings, paragraphs, `-` bullet lists, fenced code blocks,
  and links (`[text](https://...)` or a bare `https://...` URL). Bold and images are not converted yet.
- A leading `# Title` line in the body is dropped when sending, because Wix shows the title itself.

## Post types and the weekly rhythm

- `type: new` and `type: update` posts are script posts. They can go out in either slot (Tuesday or Friday).
- `type: highlight` posts are community highlights. They **only go out on Friday slots**
  (a manual "force" run ignores this). When there is no new script ready, the Friday slot can carry a
  highlight, so the blog keeps a steady rhythm.
- Every post ends with a **Community spotlight** section (see the template). In a highlight post it is the main content.

## Community spotlight rules

The lists live in `blog/community/`: `creators.md` (who and what is worth highlighting) and
`spotlight-queue.md` (what to feature next).

1. **Link and describe in your own words.** One or two original sentences about what the creator or
   resource offers. Never copy their text, and never copy Eplan's help text, manuals or training material.
2. **No borrowed images.** Do not reuse other people's thumbnails, video frames or screenshots, or Eplan
   marketing or documentation images. A screenshot you took yourself in your own Eplan, with your own
   caption, is fine (add it in Wix by hand).
3. **Credit by name and link to the source.** Say what language it is in and who it suits.
4. **Positive and specific.** Say what is useful. No comparisons that put Eplan or anyone else down, and no
   suggestion that Eplan or the community is lacking something.
5. **Verify first.** Open the link, check it is current and about Eplan, then mark it `verified` in the list.
   Do not feature something you have not opened. Nothing is posted automatically from YouTube or any feed.
6. **Not an endorsement.** A highlight means "worth a look", not that Eplan or the creator endorses this repo.

## One-time setup

1. In Wix, create an API key with Blog permissions (Account settings > API keys) and note the site id and the author's member id.
2. In GitHub: Settings > Secrets and variables > Actions. Add secrets `WIX_API_KEY`, `WIX_SITE_ID`, `WIX_MEMBER_ID`.
3. The workflow defaults to **draft** mode: it creates a draft in Wix for you to review and publish. Once you trust it, add a repository variable `WIX_MODE` = `publish`.
4. Test locally first: `python3 blog/tools/publish_wix.py --dry-run`.

`blog/published.json` is written by the workflow so a post is never sent twice.

## Status

The Wix calls were tested against the live blog in draft mode on 2026-11-07 (headings, bullets and code blocks came through correctly).
Link conversion is new and still to be checked in a draft. Keep draft mode on until you have looked at a draft with links.
