#!/usr/bin/env python3
"""List the members of your Wix site so you can find the author's member ID.

Run this locally. The key is read from an environment variable and is never
printed or stored.

PowerShell:
    $env:WIX_API_KEY  = "<paste key>"      # typed by you, this session only
    $env:WIX_SITE_ID  = "<site id>"
    python blog/tools/list_wix_members.py

The API key needs the "Read Members" permission as well as the Blog one.
Blog authors who cannot log in (guest writers) show status OFFLINE.
"""
import json
import os
import sys
import urllib.error
import urllib.request

key = os.environ.get("WIX_API_KEY", "")
site = os.environ.get("WIX_SITE_ID", "")
if not key or not site:
    sys.exit("Set WIX_API_KEY and WIX_SITE_ID in the environment first.")

offset = 0
while True:
    url = ("https://www.wixapis.com/members/v1/members"
           "?fieldsets=EXTENDED&paging.limit=50&paging.offset=%d" % offset)
    req = urllib.request.Request(
        url, headers={"Authorization": key, "wix-site-id": site})
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        sys.exit("HTTP %s: %s" % (e.code, e.read().decode("utf-8", "replace")))
    members = data.get("members", [])
    for m in members:
        print("%s  %-24s %-30s %s" % (
            m.get("id"), (m.get("profile") or {}).get("nickname", ""),
            m.get("loginEmail", ""), m.get("status", "")))
    offset += len(members)
    if not members or offset >= data.get("metadata", {}).get("total", offset):
        break
