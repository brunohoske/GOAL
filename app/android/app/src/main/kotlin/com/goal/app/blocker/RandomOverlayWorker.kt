package com.goal.app.blocker

import android.content.Context
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.Worker
import androidx.work.WorkerParameters
import java.util.concurrent.TimeUnit
import kotlin.random.Random

/**
 * "Chaos mode" random overlay: roughly every 30 minutes there's a 35% chance the block overlay
 * pops up out of nowhere to remind the member how much XP they still owe. Only fires while the
 * policy still has the random-overlay nag active (the backend already gated it to blocked +
 * within the configured day window; the app pushes that down when it syncs).
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
    private const val INTERVAL_MINUTES = 30L

    /** Starts the periodic worker when the nag is on; cancels it when off. Idempotent. */
    fun sync(context: Context, enabled: Boolean) {
        val wm = WorkManager.getInstance(context)
        if (enabled) {
            val request = PeriodicWorkRequestBuilder<RandomOverlayWorker>(
                INTERVAL_MINUTES, TimeUnit.MINUTES,
            ).build()
            wm.enqueueUniquePeriodicWork(WORK_NAME, ExistingPeriodicWorkPolicy.KEEP, request)
        } else {
            wm.cancelUniqueWork(WORK_NAME)
        }
    }
}
