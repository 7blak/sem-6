package org;

import jade.core.Profile;
import jade.core.ProfileImpl;
import jade.core.Runtime;
import jade.wrapper.ContainerController;
import org.exceptions.JadePlatformInitializationException;

import java.util.*;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import static org.JADEEngine.runAgent;
import static org.JADEEngine.runGUI;

public class Engine {
    private static final ExecutorService executor = Executors.newCachedThreadPool();
    public static int clientAgentNumber;
    public static int deliveryAgentNumber;
    public static int marketAgentNumber;

    public static void main(String[] args) {
        final Runtime runtime = Runtime.instance();
        final Profile profile = new ProfileImpl();
        profile.setParameter(Profile.MTPS, ""); // Without this parameter the JADE startup takes ~9 seconds (on my machine), now it is instant

        try {
            final ContainerController container = executor.submit(() -> runtime.createMainContainer(profile)).get();

            runGUI(container);

            Scanner scanner = new Scanner(System.in);
            System.out.println("AVAIABLE TASKS:");
            System.out.println("1 - Small sample task (similar to the one from pdf, 1 clientAgents 3 deliveryAgents 3 marketAgents)");
            System.out.println("2 - Test with multiple clients (5), delivery agents (5) and markets (6)");
            System.out.println("3 - One client with many delivery agents (5) and markets (6)");
            System.out.println("4 - ULTIMATE test (randomly generated 100 ClientAgents, DeliveryAgents, MarketAgents) each with randomized orders, stock etc.");
            System.out.println("5 - [CAREFUL] VERY ULTIMATE test (same as 4 but 1000 :p)");
            System.out.println("--------");
            System.out.print("Please enter the task number to execute: ");
            String input = scanner.nextLine();
            scanner.close();

            switch (input) {
                case "1" -> {
                    clientAgentNumber = 1;
                    deliveryAgentNumber = 3;
                    marketAgentNumber = 3;
                    runGroceryTask(container);
                }
                case "2" -> {
                    clientAgentNumber = 5;
                    deliveryAgentNumber = 5;
                    marketAgentNumber = 6;
                    runNikczemnyTestSzefa(container);
                }
                case "3" -> {
                    clientAgentNumber = 1;
                    deliveryAgentNumber = 5;
                    marketAgentNumber = 6;
                    runNikczemnyTestSzefa2(container);
                }
                case "4" -> {
                    clientAgentNumber = 100;
                    deliveryAgentNumber = 100;
                    marketAgentNumber = 100;
                    runULTIMATENIKCZEMNOSCSZEFA(container);
                }
                case "5" -> {
                    clientAgentNumber = 1000;
                    deliveryAgentNumber = 1000;
                    marketAgentNumber = 1000;
                    runABSOLUTEMAXIMUMULTIMATENIKCZEMNOSCSZEFA(container);
                }
                case "6" -> {
                    clientAgentNumber = 3;
                    deliveryAgentNumber = 1;
                    marketAgentNumber = 3;
                    runTest(container);
                }
                default -> throw new RuntimeException("Invalid task number: " + input);
            }

        } catch (final InterruptedException | ExecutionException e) {
            throw new JadePlatformInitializationException(e);
        }
    }

    private static void runTest(final ContainerController container) throws InterruptedException, ExecutionException {
        runAgent(container, "DeliveryBolt", "DeliveryAgent",
                new Object[]{5.00});
        runAgent(container, "Client1", "ClientAgent",
                new Object[]{List.of("milk", "coffee", "rice")});
        runAgent(container, "Client2", "ClientAgent",
                new Object[]{List.of("milk", "rice")});
        runAgent(container, "Client3", "ClientAgent",
                new Object[]{List.of("coffee")});
        runAgent(container, "MarketBiedronka", "MarketAgent",
                new Object[]{Map.of("milk", 5.00, "rice", 3.40)});
        runAgent(container, "MarketOsiedlowy", "MarketAgent",
                new Object[]{Map.of("rice", 1.00)});
        runAgent(container, "MarketŻabka", "MarketAgent",
                new Object[]{Map.of("coffee", 7.50, "milk", 6.39)});
    }

    private static void runGroceryTask(final ContainerController container) throws InterruptedException {
        runAgent(container, "DeliveryBolt", "DeliveryAgent",
                new Object[]{5.00});
        runAgent(container, "DeliveryUber", "DeliveryAgent",
                new Object[]{9.99});
        runAgent(container, "DeliveryWolt", "DeliveryAgent",
                new Object[]{15.50});
        runAgent(container, "Client1", "ClientAgent",
                new Object[]{List.of("milk", "coffee", "rice")});
        runAgent(container, "MarketBiedronka", "MarketAgent",
                new Object[]{Map.of("milk", 5.00, "rice", 3.40)});
        runAgent(container, "MarketOsiedlowy", "MarketAgent",
                new Object[]{Map.of("rice", 1.00)});
        runAgent(container, "MarketŻabka", "MarketAgent",
                new Object[]{Map.of("coffee", 7.50, "milk", 6.39)});
    }

    private static void runNikczemnyTestSzefa(final ContainerController mainContainer) {
        runAgent(mainContainer, "Pyszne", "DeliveryAgent", new Object[]{4.50});
        runAgent(mainContainer, "Glovo", "DeliveryAgent", new Object[]{8.75});
        runAgent(mainContainer, "DHL", "DeliveryAgent", new Object[]{12.30});
        runAgent(mainContainer, "UberEats", "DeliveryAgent", new Object[]{9.99});
        runAgent(mainContainer, "BoltFood", "DeliveryAgent", new Object[]{7.99});

        runAgent(mainContainer, "Client1", "ClientAgent", new Object[]{List.of("bread", "butter", "tea", "cheese", "wine")});
        runAgent(mainContainer, "Client2", "ClientAgent", new Object[]{List.of("milk", "coffee", "chocolate", "yogurt", "juice")});
        runAgent(mainContainer, "Client3", "ClientAgent", new Object[]{List.of("pasta", "sauce", "olive oil", "tomatoes", "parmesan")});
        runAgent(mainContainer, "Client4", "ClientAgent", new Object[]{List.of("chicken", "rice", "spices", "onions", "garlic")});
        runAgent(mainContainer, "Client5", "ClientAgent", new Object[]{List.of("fish", "lemons", "butter", "salt", "potatoes")});

        runAgent(mainContainer, "Lidl", "MarketAgent", new Object[]{Map.of("bread", 3.20, "butter", 6.50, "cheese", 12.99, "milk", 2.50, "pasta", 4.25)});
        runAgent(mainContainer, "Carrefour", "MarketAgent", new Object[]{Map.of("tea", 5.10, "butter", 7.25, "wine", 19.99, "coffee", 8.99, "rice", 3.49)});
        runAgent(mainContainer, "Auchan", "MarketAgent", new Object[]{Map.of("bread", 2.80, "tea", 4.95, "cheese", 10.50, "chocolate", 5.99, "tomatoes", 2.75)});
        runAgent(mainContainer, "Biedronka", "MarketAgent", new Object[]{Map.of("wine", 17.49, "bread", 3.10, "yogurt", 4.20, "olive oil", 14.99, "spices", 2.50)});
        runAgent(mainContainer, "Żabka", "MarketAgent", new Object[]{Map.of("butter", 6.99, "tea", 5.50, "juice", 3.75, "garlic", 1.99, "fish", 15.99)});
        runAgent(mainContainer, "AmazonFresh", "MarketAgent", new Object[]{Map.of("cheese", 11.75, "wine", 22.50, "onions", 1.50, "lemons", 2.99, "salt", 0.99)});
    }

    private static void runNikczemnyTestSzefa2(final ContainerController mainContainer) {
        // Ruthless delivery agents fighting for dominance
        runAgent(mainContainer, "Pyszne", "DeliveryAgent", new Object[]{3.99});
        runAgent(mainContainer, "Glovo", "DeliveryAgent", new Object[]{8.49});
        runAgent(mainContainer, "DHL", "DeliveryAgent", new Object[]{13.37});
        runAgent(mainContainer, "UberEats", "DeliveryAgent", new Object[]{9.95});
        runAgent(mainContainer, "BoltFood", "DeliveryAgent", new Object[]{6.66}); // The devil's price

        // The ultimate client with extravagant needs
        runAgent(mainContainer, "Client1", "ClientAgent", new Object[]{List.of(
                "golden apple", "caviar", "wagyu steak", "truffle", "champagne"
        )});

        // Merciless markets with ridiculous pricing strategies
        runAgent(mainContainer, "Lidl", "MarketAgent", new Object[]{Map.of(
                "golden apple", 99.99, "caviar", 250.00, "wagyu steak", 499.95
        )});
        runAgent(mainContainer, "Carrefour", "MarketAgent", new Object[]{Map.of(
                "truffle", 150.50, "champagne", 200.00, "wagyu steak", 450.00
        )});
        runAgent(mainContainer, "Auchan", "MarketAgent", new Object[]{Map.of(
                "golden apple", 120.00, "caviar", 275.00, "truffle", 160.99
        )});
        runAgent(mainContainer, "Biedronka", "MarketAgent", new Object[]{Map.of(
                "champagne", 175.49, "wagyu steak", 520.00, "caviar", 300.00
        )});
        runAgent(mainContainer, "Żabka", "MarketAgent", new Object[]{Map.of(
                "golden apple", 110.10, "champagne", 190.90, "truffle", 175.75
        )});
        runAgent(mainContainer, "AmazonFresh", "MarketAgent", new Object[]{Map.of(
                "golden apple", 105.99, "caviar", 290.00, "wagyu steak", 510.25, "champagne", 225.00
        )});
    }

    private static final List<String> SHOPPING_LIST = List.of(
            "golden apple", "caviar", "wagyu steak", "truffle", "champagne",
            "lobster", "black garlic", "saffron", "matsutake mushrooms", "aged balsamic vinegar",
            "beluga caviar", "kobe beef", "foie gras", "white truffle", "bluefin tuna",
            "artisanal honey", "persian saffron", "pink Himalayan salt", "oysters",
            "pata negra ham", "bird’s nest soup", "wasabi root", "handmade chocolate",
            "diamond-infused water", "moon cheese", "century egg", "exotic dragonfruit",
            "dinosaur meat", "ghost pepper sauce", "meteorite dust seasoning",
            "nebula grapes", "enchanted sugar", "emerald lettuce", "platinum potatoes",
            "mystic mango", "quantum quinoa", "truffle-infused caviar", "galaxy macarons",
            "void strawberries", "levitating pancakes", "invisible coffee", "aged phoenix egg"
    );

    private static final Random RAND = new Random();

    private static List<String> getRandomSubset(int size) {
        List<String> shuffled = new ArrayList<>(SHOPPING_LIST);
        Collections.shuffle(shuffled, RAND);
        return shuffled.subList(0, Math.min(size, shuffled.size()));
    }

    private static Map<String, Double> getRandomPricedSubset(int size) {
        Map<String, Double> productPrices = new HashMap<>();
        for (String item : getRandomSubset(size)) {
            productPrices.put(item, RAND.nextDouble() * 500 + 20); // Prices between 20 and 520
        }
        return productPrices;
    }

    private static void runULTIMATENIKCZEMNOSCSZEFA(final ContainerController mainContainer) {
        // LEGION of delivery agents with random prices
        for (int i = 1; i <= 100; i++) {
            runAgent(mainContainer, "DeliveryAgent" + i, "DeliveryAgent", new Object[]{(RAND.nextDouble() * 50) + 5.00});
        }

        // CLIENTS flooding the system with demands
        for (int i = 1; i <= 100; i++) {
            runAgent(mainContainer, "Client" + i, "ClientAgent", new Object[]{getRandomSubset(RAND.nextInt(15) + 5)});
        }

        // MARKETS with randomly priced products
        for (int i = 1; i <= 100; i++) {
            runAgent(mainContainer, "Market" + i, "MarketAgent", new Object[]{getRandomPricedSubset(RAND.nextInt(10) + 5)});
        }
    }

    private static void runABSOLUTEMAXIMUMULTIMATENIKCZEMNOSCSZEFA(final ContainerController mainContainer) {
        // LEGION of delivery agents with random prices
        for (int i = 1; i <= 1000; i++) {
            runAgent(mainContainer, "DeliveryAgent" + i, "DeliveryAgent", new Object[]{(RAND.nextDouble() * 50) + 5.00});
        }

        // CLIENTS flooding the system with demands
        for (int i = 1; i <= 1000; i++) {
            runAgent(mainContainer, "Client" + i, "ClientAgent", new Object[]{getRandomSubset(RAND.nextInt(15) + 5)});
        }

        // MARKETS with randomly priced products
        for (int i = 1; i <= 1000; i++) {
            runAgent(mainContainer, "Market" + i, "MarketAgent", new Object[]{getRandomPricedSubset(RAND.nextInt(10) + 5)});
        }
    }
}
