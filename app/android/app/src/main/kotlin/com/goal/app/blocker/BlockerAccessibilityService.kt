package com.goal.app.blocker

import android.accessibilityservice.AccessibilityService
import android.view.accessibility.AccessibilityEvent

/**
 * Enforces the block policy in two ways:
 *  1. App blocking — a blocked app comes to the foreground (TYPE_WINDOW_STATE_CHANGED).
 *  2. Site blocking — a known browser shows a blocked domain in its URL bar
 *     (TYPE_WINDOW_CONTENT_CHANGED, throttled). Domains derive from the blocked packages.
 *
 * On a hit it shows a full-screen overlay explaining why and sends the user home.
 */
class BlockerAccessibilityService : AccessibilityService() {

    companion object {
        // Allows the overlay to notify the running Flutter engine for telemetry.
        var onBlockedAppOpened: ((String) -> Unit)? = null

        private const val BROWSER_CHECK_INTERVAL_MS = 400L
    }

    private var lastBrowserCheckAt = 0L

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        if (event == null) return
        val packageName = event.packageName?.toString() ?: return

        // Ignore our own UI.
        if (packageName == applicationContext.packageName) return

        val policy = BlockPolicyStore.load(applicationContext)
        if (!policy.enabled) return

        when (event.eventType) {
            AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED -> {
                if (policy.packages.contains(packageName)) {
                    block(policy, packageName)
                } else {
                    maybeBlockBrowser(packageName, policy)
                }
            }
            AccessibilityEvent.TYPE_WINDOW_CONTENT_CHANGED ->
                maybeBlockBrowser(packageName, policy)
            else -> Unit
        }
    }

    /** If [packageName] is a known browser showing a blocked domain, enforce the block. */
    private fun maybeBlockBrowser(packageName: String, policy: BlockPolicyStore.Policy) {
        val urlBarId = BrowserBlocklist.browsers[packageName] ?: return
        val domains = BrowserBlocklist.domainsFor(policy.packages)
        if (domains.isEmpty()) return

        // Content events fire constantly while browsing; check at most every 400ms.
        val now = System.currentTimeMillis()
        if (now - lastBrowserCheckAt < BROWSER_CHECK_INTERVAL_MS) return
        lastBrowserCheckAt = now

        val root = rootInActiveWindow ?: return
        val urlText = try {
            root.findAccessibilityNodeInfosByViewId(urlBarId)?.firstOrNull()?.text?.toString()
        } catch (_: Exception) {
            null
        } ?: return

        val host = BrowserBlocklist.hostOf(urlText) ?: return
        if (BrowserBlocklist.isBlockedHost(host, domains)) {
            block(policy, "$packageName:$host")
        }
    }

    private fun block(policy: BlockPolicyStore.Policy, source: String) {
        BlockOverlay.show(applicationContext, policy)
        onBlockedAppOpened?.invoke(source)
        performGlobalAction(GLOBAL_ACTION_HOME)
    }

    override fun onInterrupt() { /* no-op */ }
}
