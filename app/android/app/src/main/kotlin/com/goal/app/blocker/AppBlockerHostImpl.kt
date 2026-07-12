package com.goal.app.blocker

import android.app.AppOpsManager
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Process
import android.provider.Settings
import android.text.TextUtils

/**
 * Implements the Pigeon-generated [AppBlockerHost]. Bridges Flutter calls to the native enforcer:
 * persists the policy, checks/opens the special permissions blocking requires.
 */
class AppBlockerHostImpl(private val context: Context) : AppBlockerHost {

    override fun applyPolicy(policy: BlockPolicy) {
        BlockPolicyStore.save(
            context,
            BlockPolicyStore.Policy(
                enabled = policy.enabled,
                packages = policy.packages.filterNotNull().toSet(),
                targetPct = policy.targetPct.toInt(),
                currentPct = policy.currentPct.toInt(),
                xpRemaining = policy.xpRemaining.toInt(),
                goalTitle = policy.goalTitle,
            ),
        )
    }

    override fun clearPolicy() = BlockPolicyStore.clear(context)

    override fun hasAccessibilityPermission(): Boolean {
        val expected = "${context.packageName}/${BlockerAccessibilityService::class.java.name}"
        val enabled = Settings.Secure.getString(
            context.contentResolver,
            Settings.Secure.ENABLED_ACCESSIBILITY_SERVICES,
        ) ?: return false
        val splitter = TextUtils.SimpleStringSplitter(':').apply { setString(enabled) }
        while (splitter.hasNext()) {
            if (splitter.next().equals(expected, ignoreCase = true)) return true
        }
        return false
    }

    override fun hasOverlayPermission(): Boolean =
        Build.VERSION.SDK_INT < Build.VERSION_CODES.M || Settings.canDrawOverlays(context)

    override fun hasUsageAccessPermission(): Boolean {
        val appOps = context.getSystemService(Context.APP_OPS_SERVICE) as AppOpsManager
        val mode = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            appOps.unsafeCheckOpNoThrow(
                AppOpsManager.OPSTR_GET_USAGE_STATS, Process.myUid(), context.packageName,
            )
        } else {
            @Suppress("DEPRECATION")
            appOps.checkOpNoThrow(
                AppOpsManager.OPSTR_GET_USAGE_STATS, Process.myUid(), context.packageName,
            )
        }
        return mode == AppOpsManager.MODE_ALLOWED
    }

    override fun openAccessibilitySettings() =
        startSettings(Settings.ACTION_ACCESSIBILITY_SETTINGS)

    override fun openOverlaySettings() = startSettings(
        Settings.ACTION_MANAGE_OVERLAY_PERMISSION,
        Uri.parse("package:${context.packageName}"),
    )

    override fun openUsageAccessSettings() =
        startSettings(Settings.ACTION_USAGE_ACCESS_SETTINGS)

    private fun startSettings(action: String, data: Uri? = null) {
        val intent = Intent(action).apply {
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            if (data != null) this.data = data
        }
        context.startActivity(intent)
    }
}
