package org.labs.behaviours;

import jade.core.Agent;
import jade.core.behaviours.OneShotBehaviour;
import jade.core.behaviours.TickerBehaviour;

import java.util.concurrent.atomic.AtomicInteger;

public class SimpleCountBehaviour extends TickerBehaviour {

    private static final AtomicInteger count = new AtomicInteger(0);
    public SimpleCountBehaviour(final Agent a, long period) {
        super(a, period);
    }

    @Override
    protected void onTick() {
        System.out.printf("Counter: %d\n", count.getAndIncrement());

        if(count.get() > 5) {
            stop();
        }
    }
}
