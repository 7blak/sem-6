package org.labs.laboratory1.agents;

import org.labs.laboratory1.behaviours.SimpleCountingBehaviour;
import org.labs.laboratory1.behaviours.TickerCountingBehaviour;

import jade.core.Agent;

/**
 * Example of a simple agent that aims to demonstrate the agent life-cycle
 */
public class FirstAgent extends Agent {

	public FirstAgent() {
		System.out.printf("Agent %s is being created. My state is: %s. %n",
				getName(),
				getAgentState().getName());
	}

	@Override
	protected void setup() {
		System.out.printf("Agent %s was initialized. My state is: %s. %n",
				getName(),
				getAgentState().getName());

		demonstrationOfSimpleBehaviours();
		demonstrationOfTickerBehaviour();
	}

	@Override
	protected void takeDown() {
		System.out.printf("Agent %s is being destroyed. My state is: %s. %n",
				getName(),
				getAgentState().getName());
	}

	private void demonstrationOfSimpleBehaviours() {
		addBehaviour(new SimpleCountingBehaviour(1));
		addBehaviour(new SimpleCountingBehaviour(2));
		addBehaviour(new SimpleCountingBehaviour(3));
	}

	private void demonstrationOfTickerBehaviour() {
		addBehaviour(new TickerCountingBehaviour(this, 2000));
	}
}
