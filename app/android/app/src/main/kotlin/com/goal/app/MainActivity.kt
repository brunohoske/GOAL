package com.goal.app

import com.goal.app.blocker.AppBlockerEvents
import com.goal.app.blocker.AppBlockerHost
import com.goal.app.blocker.AppBlockerHostImpl
import com.goal.app.blocker.BlockerAccessibilityService
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine

class MainActivity : FlutterActivity() {

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        // Register the Pigeon host so Flutter can drive the native blocker.
        AppBlockerHost.setUp(flutterEngine.dartExecutor.binaryMessenger, AppBlockerHostImpl(applicationContext))

        // Forward blocked-app events to Flutter for telemetry.
        val events = AppBlockerEvents(flutterEngine.dartExecutor.binaryMessenger)
        BlockerAccessibilityService.onBlockedAppOpened = { pkg ->
            runOnUiThread { events.onBlockedAppOpened(pkg) {} }
        }
    }
}
