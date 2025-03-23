package org.labs.laboratory1.behaviours;

import jade.core.behaviours.OneShotBehaviour;

/**
 * Example demonstrating how to create behaviour
 */
public class SimpleCountingBehaviour extends OneShotBehaviour {

	final int counter;

	public SimpleCountingBehaviour(int counter) {
		this.counter = counter;
	}

	@Override
	public void action() {
		System.out.printf("[Simple] Current counter: %d %n", counter);
	}
}
