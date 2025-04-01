package org.labs;

import jade.core.Profile;
import jade.core.ProfileImpl;
import jade.core.Runtime;
import jade.wrapper.AgentController;
import jade.wrapper.ContainerController;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class Main {
    private static final ExecutorService jadeExecutor = Executors.newCachedThreadPool();

    public static void main(String[] args) {
        final Runtime runtime = Runtime.instance();
        final Profile profile = new ProfileImpl();

        try {
            final ContainerController container = jadeExecutor.submit(() -> runtime.createMainContainer(profile)).get();

            final AgentController agentController = container.createNewAgent("Agent1","org.labs.agents.FirstAgent", new Object[] {});
            final AgentController agentController2 = container.createNewAgent("rma", "jade.tools.rma.rma", new Object[] {});

            agentController2.start();
            agentController.start();
        } catch (Exception e) {
            e.printStackTrace();
        }
        }
    }