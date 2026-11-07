#!/usr/bin/env python3
"""Email the review copy of the next blog post.

Usage:  python3 blog/tools/send_preview.py DIR
        python3 blog/tools/send_preview.py --test    (sends a short test message)

DIR holds subject.txt and body.txt, written by `publish_wix.py --preview DIR`.
If DIR has no files, nothing is sent.

Environment variables (set as GitHub secrets):
    SMTP_HOST      mail server, for example smtp.gmail.com
    SMTP_PORT      optional, default 587 (STARTTLS)
    SMTP_USER      login name for the mail server
    SMTP_PASSWORD  password or app password
    REVIEW_EMAIL   where the review copy is sent

Only the Python standard library is used. Nothing is printed except a short
status line; credentials are never written to the log.
"""
import os
import smtplib
import sys
from email.message import EmailMessage


def main():
    if len(sys.argv) != 2:
        sys.exit("usage: send_preview.py DIR | --test")
    test = sys.argv[1] == "--test"
    subject_path = os.path.join(sys.argv[1], "subject.txt")
    body_path = os.path.join(sys.argv[1], "body.txt")
    if not test and not (os.path.exists(subject_path) and os.path.exists(body_path)):
        print("No preview to send.")
        return 0

    host = os.environ.get("SMTP_HOST", "")
    user = os.environ.get("SMTP_USER", "")
    password = os.environ.get("SMTP_PASSWORD", "")
    to = os.environ.get("REVIEW_EMAIL", "")
    if not (host and user and password and to):
        print("Email settings are not set; skipping the review email.")
        return 0

    if test:
        subject = "Blog review email: test"
        body = ("This is a test of the blog review email. If you can read it, "
                "the mail settings work. Nothing was published.\n")
    else:
        with open(subject_path, encoding="utf-8") as f:
            subject = f.read().strip()
        with open(body_path, encoding="utf-8") as f:
            body = f.read()

    msg = EmailMessage()
    msg["Subject"] = subject
    msg["From"] = user
    msg["To"] = to
    msg.set_content(body)

    port = int(os.environ.get("SMTP_PORT") or 587)
    with smtplib.SMTP(host, port, timeout=60) as smtp:
        smtp.starttls()
        smtp.login(user, password)
        smtp.send_message(msg)
    print("Review email sent.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
