package org;

import static org.JADEEngine.runGUI;
import static org.JADEEngine.runAgent;

import jade.core.Profile;
import jade.core.ProfileImpl;
import jade.core.Runtime;
import jade.wrapper.ContainerController;
import org.exceptions.JadePlatformInitializationException;

import java.util.List;
import java.util.Map;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class Engine {
    private static final ExecutorService executor = Executors.newCachedThreadPool();

    public static void main(String[] args) {
        final Runtime runtime = Runtime.instance();
        final Profile profile = new ProfileImpl();
        profile.setParameter(Profile.MTPS, ""); // Without this parameter the JADE startup takes ~9 seconds (on my machine), now it is instant

        try {
            final ContainerController container = executor.submit(() -> runtime.createMainContainer(profile)).get();

            runGUI(container);
            runGroceryTask(container);
        } catch (final InterruptedException | ExecutionException e) {
            throw new JadePlatformInitializationException(e);
        }
    }

    private static void runGroceryTask(final ContainerController container) throws InterruptedException {
        runAgent(container, "DeliveryBolt", "DeliveryAgent", "agents",
                new Object[] {5.00});
        runAgent(container, "DeliveryUber", "DeliveryAgent", "agents",
                new Object[] {9.99});
        runAgent(container, "DeliveryWolt", "DeliveryAgent", "agents",
                new Object[] {15.50});
        runAgent(container, "Client1", "ClientAgent", "agents",
                new Object[] {List.of("milk", "coffee", "rice")});
        runAgent(container, "MarketBiedronka", "MarketAgent", "agents",
                new Object[] {Map.of("milk", 5.00, "rice", 3.40 )});
        runAgent(container, "MarketOsiedlowy", "MarketAgent", "agents",
                new Object[] {Map.of("rice", 1.00)});
        runAgent(container, "MarketŻabka", "MarketAgent", "agents",
                new Object[] {Map.of("coffee", 7.50, "milk", 6.39, "rice", 4.10)});
    }
}
