package com.goal.app.blocker

import android.content.Context
import android.graphics.Color
import android.graphics.PixelFormat
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.provider.Settings
import android.view.Gravity
import android.view.View
import android.view.WindowManager
import android.widget.LinearLayout
import android.widget.TextView

/**
 * A lightweight full-screen overlay shown over a blocked app. Built in code (no XML) to keep the
 * native footprint minimal. Auto-dismisses after a short delay; the service has already sent the
 * user home, so this is a brief "why am I blocked" message.
 */
object BlockOverlay {

    private var shownAt = 0L
    private const val MIN_INTERVAL_MS = 1500L // avoid flicker on rapid window events

    /**
     * Shows the overlay. [force] bypasses the anti-flicker throttle — used by the random
     * chaos-mode nag, which intentionally appears on its own schedule. [title]/[message]
     * default to the generic "you are blocked" copy; partial blocks (e.g. Shorts) customize them.
     */
    fun show(
        context: Context,
        policy: BlockPolicyStore.Policy,
        force: Boolean = false,
        title: String? = null,
        message: String? = null,
    ) {
        if (!canDrawOverlays(context)) return
        val now = System.currentTimeMillis()
        if (!force && now - shownAt < MIN_INTERVAL_MS) return
        shownAt = now

        val wm = context.getSystemService(Context.WINDOW_SERVICE) as WindowManager
        val view = buildView(context, policy, title, message)

        val type = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
            WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY
        else
            @Suppress("DEPRECATION") WindowManager.LayoutParams.TYPE_PHONE

        val params = WindowManager.LayoutParams(
            WindowManager.LayoutParams.MATCH_PARENT,
            WindowManager.LayoutParams.MATCH_PARENT,
            type,
            WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE or
                WindowManager.LayoutParams.FLAG_NOT_TOUCH_MODAL,
            PixelFormat.OPAQUE,
        )

        try {
            wm.addView(view, params)
            Handler(Looper.getMainLooper()).postDelayed({
                try { wm.removeView(view) } catch (_: Exception) {}
            }, 2500)
        } catch (_: Exception) { /* overlay may fail if permission revoked mid-flight */ }
    }

    private fun buildView(
        context: Context,
        policy: BlockPolicyStore.Policy,
        customTitle: String?,
        customMessage: String?,
    ): View {
        val root = LinearLayout(context).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.CENTER
            setBackgroundColor(Color.parseColor("#FCFBFA")) // warm off-white (matches app)
            setPadding(64, 64, 64, 64)
        }

        val brand = TextView(context).apply {
            text = "GOAL"
            setTextColor(Color.parseColor("#5B5BD6"))
            textSize = 28f
            letterSpacing = 0.1f
            gravity = Gravity.CENTER
        }
        val title = TextView(context).apply {
            text = customTitle ?: "Você está bloqueado"
            setTextColor(Color.parseColor("#1A1B25"))
            textSize = 22f
            gravity = Gravity.CENTER
            setPadding(0, 48, 0, 12)
        }
        val message = TextView(context).apply {
            text = customMessage
                ?: "Faltam ${policy.xpRemaining} XP para liberar seus apps em \"${policy.goalTitle}\"."
            setTextColor(Color.parseColor("#6B6E80"))
            textSize = 15f
            gravity = Gravity.CENTER
        }

        root.addView(brand)
        root.addView(title)
        root.addView(message)
        return root
    }

    private fun canDrawOverlays(context: Context): Boolean =
        Build.VERSION.SDK_INT < Build.VERSION_CODES.M || Settings.canDrawOverlays(context)
}
