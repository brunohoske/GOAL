package com.goal.app.blocker

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

/**
 * After reboot the persisted policy is already in SharedPreferences and the AccessibilityService
 * (if the user enabled it) is restarted by the system, so blocking resumes automatically.
 * This receiver is a hook for any future re-arming (e.g. rescheduling notification work).
 */
class BootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action == Intent.ACTION_BOOT_COMPLETED) {
            // Policy persists across reboot; nothing else required for now.
            BlockPolicyStore.load(context)
        }
    }
}
