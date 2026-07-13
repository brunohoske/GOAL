package com.goal.app.blocker

/**
 * Detects the YouTube Shorts player so Shorts can be blocked WITHOUT blocking YouTube itself.
 * Goals store the [PSEUDO_PACKAGE] below in their blocked-apps list; the accessibility service
 * then bounces the user out of the Shorts player (app) and off youtube.com/shorts (browser)
 * while regular videos stay available.
 */
object ShortsBlocklist {
    const val YOUTUBE_PACKAGE = "com.google.android.youtube"
    const val PSEUDO_PACKAGE = "$YOUTUBE_PACKAGE:shorts"

    /**
     * View ids that only exist while the Shorts player/feed is on screen (absent from the
     * regular video player). Not a public API — YouTube updates may rename them, so several
     * known candidates are checked; lookup by id is an indexed search, not a tree walk.
     */
    val playerViewIds = listOf(
        "$YOUTUBE_PACKAGE:id/reel_recycler",
        "$YOUTUBE_PACKAGE:id/reel_watch_player",
        "$YOUTUBE_PACKAGE:id/reel_player_page_container",
        "$YOUTUBE_PACKAGE:id/reel_progress_bar",
    )

    fun isEnabled(blockedPackages: Set<String>): Boolean = PSEUDO_PACKAGE in blockedPackages

    /**
     * True when a browser URL bar shows a YouTube Shorts page. Path-aware, unlike the
     * host-only site blocking in [BrowserBlocklist] — youtube.com stays reachable.
     */
    fun isShortsUrl(urlBarText: String): Boolean {
        var t = urlBarText.trim().lowercase()
        if (t.isEmpty() || t.contains(' ')) return false
        t = t.removePrefix("https://").removePrefix("http://")
        t = t.removePrefix("www.").removePrefix("m.")
        return t.startsWith("youtube.com/shorts")
    }
}
