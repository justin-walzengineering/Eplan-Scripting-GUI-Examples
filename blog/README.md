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
  and links (`[text](https://...)` or a bare `https://...` URL). Bold (`**text**`) and images are not converted yet, so do not use `**` in posts.
- A leading `# Title` line in the body is dropped when sending, because Wix shows the title itself.

## Post types and the weekly rhythm

- `type: new` and `type: update` posts are script posts. They can go out in either slot (Tuesday or Friday).
- `type: highlight` posts are community highlights. A highlight goes out **only on a Friday, and only when
  no script post is ready**. If a new or update post is waiting, it goes first (in either slot) and the
  highlight waits. A manual "force" run ignores the Friday check.

## Review email (one hour before release)

At 8 AM (blog time zone) on Tuesday and Friday, the workflow emails the next post to the reviewer so it can
be checked and edited before the 9 AM release. The email has the full text, a link to edit the file on GitHub,
and how to hold it back (change `status: ready` to `status: draft`). Edits must be committed to `main` before 9 AM.
If no post is due, no email is sent.

Add these repository **secrets** (Settings > Secrets and variables > Actions): `SMTP_HOST`, `SMTP_PORT`
(optional, default 587), `SMTP_USER`, `SMTP_PASSWORD`, `REVIEW_EMAIL`. Without them the email is skipped and
publishing is unaffected. The reviewer's address is kept in a secret, not in the public repo.
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
Link conversion was checked in a Wix draft on 2026-11-07 and looks right.

## Future improvements

- **Grow and review the creators list (Justin).** `blog/community/creators.md` starts small on purpose.
  Come back to it to:
  - open each entry marked `check` (Eplan's YouTube channel link, the events and webcasts page, EPLAN in a Nutshell,
    the Eplan blog index) and change it to `verified`;
  - add independent creators and community projects you have watched and liked, such as the "How To Eplan"
    YouTube channel and the EasyEPLANner project on GitHub;
  - keep `spotlight-queue.md` stocked, with a few verified items ready ahead of each Friday highlight.
  Follow the Community spotlight rules above (own words, no borrowed images, verify first).
- **Check command (not built yet).** List examples whose files changed in git since their blog post was sent,
  to trigger `update` posts.
