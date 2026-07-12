package com.goal.app.blocker

/**
 * Closes the browser loophole: knows how to find the URL bar of popular Android browsers
 * in the accessibility tree, and which web domains correspond to each blockable app.
 * Domains are DERIVED from the goal's blocked packages, so whatever the group chose to
 * block is also blocked on the web — no extra configuration.
 *
 * Privacy: only the address-bar text is inspected, in memory, to compare the host against
 * the blocked domains. Nothing is stored or transmitted.
 */
object BrowserBlocklist {

    /** Browser package -> resource id of its URL bar. */
    val browsers: Map<String, String> = mapOf(
        "com.android.chrome" to "com.android.chrome:id/url_bar",
        "com.chrome.beta" to "com.chrome.beta:id/url_bar",
        "com.chrome.dev" to "com.chrome.dev:id/url_bar",
        "com.microsoft.emmx" to "com.microsoft.emmx:id/url_bar",
        "com.brave.browser" to "com.brave.browser:id/url_bar",
        "com.opera.browser" to "com.opera.browser:id/url_field",
        "com.opera.mini.native" to "com.opera.mini.native:id/url_field",
        "org.mozilla.firefox" to "org.mozilla.firefox:id/mozac_browser_toolbar_url_view",
        "com.sec.android.app.sbrowser" to "com.sec.android.app.sbrowser:id/location_bar_edit_text",
        "com.duckduckgo.mobile.android" to "com.duckduckgo.mobile.android:id/omnibarTextInput",
    )

    /** Blocked app package -> the domains of its web version. */
    private val packageDomains: Map<String, List<String>> = mapOf(
        "com.instagram.android" to listOf("instagram.com"),
        "com.zhiliaoapp.musically" to listOf("tiktok.com"),
        "com.twitter.android" to listOf("twitter.com", "x.com"),
        "com.facebook.katana" to listOf("facebook.com", "fb.com", "fb.watch"),
        "com.google.android.youtube" to listOf("youtube.com", "youtu.be"),
    )

    fun domainsFor(blockedPackages: Set<String>): Set<String> =
        blockedPackages.flatMap { packageDomains[it] ?: emptyList() }.toSet()

    /**
     * Extracts the host from whatever the URL bar shows — browsers display anything from
     * "instagram.com/reels" to a full "https://www.instagram.com/...". Returns null for
     * text that is clearly not a URL (e.g. a search query being typed).
     */
    fun hostOf(urlBarText: String): String? {
        var t = urlBarText.trim().lowercase()
        if (t.isEmpty() || t.contains(' ')) return null
        t = t.removePrefix("https://").removePrefix("http://")
        t = t.substringBefore('/').substringBefore(':')
        t = t.removePrefix("www.")
        if (t.isEmpty() || !t.contains('.')) return null
        return t
    }

    fun isBlockedHost(host: String, domains: Set<String>): Boolean =
        domains.any { host == it || host.endsWith(".$it") }
}
