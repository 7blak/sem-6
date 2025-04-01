package org.labs.laboratory1.behaviours;

import java.util.concurrent.atomic.AtomicInteger;

import jade.core.Agent;
import jade.core.behaviours.TickerBehaviour;

/**
 * Example demonstrating how TickerBehaviour works
 */
public class TickerCountingBehaviour extends TickerBehaviour {

	final AtomicInteger counter = new AtomicInteger(1);

	public TickerCountingBehaviour(final Agent a, final long period) {
		super(a, period);
	}

	@Override
	protected void onTick() {
		System.out.printf("[Ticker] Current counter: %d %n", counter.getAndIncrement());

		if (counter.get() > 5) {
			System.out.println("[Ticker] Counting finished!");
			stop();
		}
	}
}
