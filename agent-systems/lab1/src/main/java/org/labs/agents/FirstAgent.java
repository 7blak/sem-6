package org.labs.agents;

import jade.core.Agent;
import jade.core.behaviours.SimpleBehaviour;
import org.labs.behaviours.SimpleCountBehaviour;

public class FirstAgent extends Agent {
    public FirstAgent() {
        System.out.printf("Agent %s is being created. My state is: %s\n", getName(), getAgentState().getName());
    }

    @Override
    protected void setup() {
        System.out.printf("Agent %s is being initiated. My state is: %s\n", getName(), getAgentState().getName());

        addBehaviour(new SimpleCountBehaviour(this, 2000));
    }

    @Override
    protected void takeDown() {
        System.out.printf("Agent %s is being destroyed. My state is: %s\n", getName(), getAgentState().getName());
    }
}
