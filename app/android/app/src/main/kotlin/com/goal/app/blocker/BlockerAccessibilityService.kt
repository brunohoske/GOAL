package com.goal.app.blocker

import android.accessibilityservice.AccessibilityService
import android.os.Bundle
import android.view.accessibility.AccessibilityEvent
import android.view.accessibility.AccessibilityNodeInfo
import kotlin.random.Random

/**
 * Enforces the block policy in three ways:
 *  1. App blocking — a blocked app comes to the foreground (TYPE_WINDOW_STATE_CHANGED).
 *  2. Site blocking — a known browser shows a blocked domain in its URL bar
 *     (TYPE_WINDOW_CONTENT_CHANGED, throttled). Domains derive from the blocked packages.
 *  3. Typing sabotage (chaos mode) — while active, each text change in any app has a ~7%
 *     chance of being replaced by the goal's custom message (TYPE_VIEW_TEXT_CHANGED).
 *
 * On a block hit it shows a full-screen overlay explaining why and sends the user home.
 * Only the address bar / edited field text is inspected; nothing is stored or transmitted.
 */
class BlockerAccessibilityService : AccessibilityService() {

    companion object {
        // Allows the overlay to notify the running Flutter engine for telemetry.
        var onBlockedAppOpened: ((String) -> Unit)? = null

        private const val BROWSER_CHECK_INTERVAL_MS = 400L
        private const val TYPING_SABOTAGE_CHANCE = 7 // percent, per text-change event
    }

    private var lastBrowserCheckAt = 0L

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        if (event == null) return
        val packageName = event.packageName?.toString() ?: return

        // Ignore our own UI.
        if (packageName == applicationContext.packageName) return

        val policy = BlockPolicyStore.load(applicationContext)
        if (!policy.hasAnyEnforcement) return

        when (event.eventType) {
            AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED -> {
                if (policy.enabled && policy.packages.contains(packageName)) {
                    block(policy, packageName)
                } else if (policy.enabled) {
                    maybeBlockBrowser(packageName, policy)
                }
            }
            AccessibilityEvent.TYPE_WINDOW_CONTENT_CHANGED ->
                if (policy.enabled) maybeBlockBrowser(packageName, policy)
            AccessibilityEvent.TYPE_VIEW_TEXT_CHANGED ->
                if (policy.typingSabotageEnabled) maybeSabotageTyping(event, policy)
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

    /**
     * Chaos mode: on a random ~7% of keystrokes, replace the edited field's text with the
     * goal's custom message (with {xp}/{nome} already resolved by the backend). Only touches
     * editable fields; the caret jumps to the end so the user keeps fighting it.
     */
    private fun maybeSabotageTyping(event: AccessibilityEvent, policy: BlockPolicyStore.Policy) {
        val message = policy.typingSabotageText
        if (message.isBlank()) return
        if (Random.nextInt(100) >= TYPING_SABOTAGE_CHANCE) return

        val node = event.source ?: return
        if (!node.isEditable) return
        // Avoid an infinite loop: if it already shows our message, leave it.
        if (node.text?.toString() == message) return

        try {
            val args = Bundle().apply {
                putCharSequence(AccessibilityNodeInfo.ACTION_ARGUMENT_SET_TEXT_CHARSEQUENCE, message)
            }
            node.performAction(AccessibilityNodeInfo.ACTION_SET_TEXT, args)
        } catch (_: Exception) {
            // Some apps use custom inputs that reject SET_TEXT — ignore.
        }
    }

    private fun block(policy: BlockPolicyStore.Policy, source: String) {
        BlockOverlay.show(applicationContext, policy)
        onBlockedAppOpened?.invoke(source)
        performGlobalAction(GLOBAL_ACTION_HOME)
    }

    override fun onInterrupt() { /* no-op */ }
}
