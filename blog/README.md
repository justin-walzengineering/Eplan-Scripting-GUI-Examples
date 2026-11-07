# Blog posts

Markdown posts that a GitHub Actions workflow sends to the Wix blog on schedule.
This is an unofficial community resource; keep posts educational and original
(see the root `CLAUDE.md` hard rules).

## Writing a post

Add `blog/posts/YYYY-MM-DD-short-name.md`:

```
---
title: Post title
publish_at: 2026-11-20T09:00:00-05:00
status: draft
excerpt: Optional one-sentence summary
---

Body in Markdown.
```

- `status: draft` is never sent. Change it to `ready` when the post is final.
- `publish_at` needs a UTC offset. The workflow runs hourly, so a post goes out
  within about an hour after that time.
- Supported Markdown: `#`/`##`/`###` headings, paragraphs, `-` bullet lists, fenced code blocks.
  Bold, links and images are not converted yet.

## One-time setup

1. In Wix, create an API key with Blog permissions (Account settings > API keys) and note the site id and the author's member id.
2. In GitHub: Settings > Secrets and variables > Actions. Add secrets `WIX_API_KEY`, `WIX_SITE_ID`, `WIX_MEMBER_ID`.
3. The workflow defaults to **draft** mode: it creates a draft in Wix for you to review and publish. Once you trust it, add a repository variable `WIX_MODE` = `publish`.
4. Test locally first: `python3 blog/tools/publish_wix.py --dry-run`.

`blog/published.json` is written by the workflow so a post is never sent twice.

## Status

The Wix API calls have **not** been tested against a live site yet. Run draft mode first and check the result in Wix.
