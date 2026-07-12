package com.goal.app.blocker

import android.content.Context
import androidx.work.Configuration
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.Worker
import androidx.work.WorkerParameters
import java.util.concurrent.TimeUnit
import kotlin.random.Random

/**
 * "Chaos mode" random overlay: on each tick there's a 35% chance the block overlay pops up out
 * of nowhere to remind the member how much XP they still owe. We request a 10-minute period, but
 * Android clamps periodic work to a 15-minute minimum, so in practice it fires roughly every
 * 15 minutes. Only fires while the policy still has the random-overlay nag active (the backend
 * already gated it to blocked + within the configured day window; the app pushes that down when
 * it syncs).
 */
class RandomOverlayWorker(context: Context, params: WorkerParameters) : Worker(context, params) {

    override fun doWork(): Result {
        val policy = BlockPolicyStore.load(applicationContext)
        if (!policy.randomOverlayEnabled) {
            RandomOverlayScheduler.sync(applicationContext, false)
            return Result.success()
        }

        if (Random.nextInt(100) < RandomOverlayScheduler.CHANCE_PERCENT) {
            BlockOverlay.show(applicationContext, policy, force = true)
        }
        return Result.success()
    }
}

object RandomOverlayScheduler {
    const val CHANCE_PERCENT = 35
    private const val WORK_NAME = "goal_random_overlay"
    private const val INTERVAL_MINUTES = 10L

    /** Starts the periodic worker when the nag is on; cancels it when off. Idempotent. */
    fun sync(context: Context, enabled: Boolean) {
        val wm = workManager(context)
        if (enabled) {
            val request = PeriodicWorkRequestBuilder<RandomOverlayWorker>(
                INTERVAL_MINUTES, TimeUnit.MINUTES,
            ).build()
            wm.enqueueUniquePeriodicWork(WORK_NAME, ExistingPeriodicWorkPolicy.KEEP, request)
        } else {
            wm.cancelUniqueWork(WORK_NAME)
        }
    }

    /**
     * We disabled WorkManager's automatic startup (it was crashing the app on launch), so we
     * initialize it on demand here. initialize() throws if already initialized — hence the guard.
     */
    private fun workManager(context: Context): WorkManager {
        if (!WorkManager.isInitialized()) {
            WorkManager.initialize(context.applicationContext, Configuration.Builder().build())
        }
        return WorkManager.getInstance(context.applicationContext)
    }
}
