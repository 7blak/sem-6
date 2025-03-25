package org.behaviours.delivery;

import jade.core.AID;
import jade.core.behaviours.Behaviour;
import jade.lang.acl.ACLMessage;
import jade.lang.acl.MessageTemplate;
import org.Util;
import org.agents.DeliveryAgent;

import java.util.*;

public class QueryMarketsBehaviour extends Behaviour {
    private final DeliveryAgent _deliveryAgent;
    private final long startTime;
    private final Map<AID, Map<String, Double>> marketStocks = new HashMap<>();
    private boolean queriesSent = false;

    public QueryMarketsBehaviour(DeliveryAgent deliveryAgent) {
        super(deliveryAgent);
        _deliveryAgent = deliveryAgent;
        startTime = System.currentTimeMillis();
    }

    @Override
    public void action() {
        if (!queriesSent) {
            ACLMessage query = new ACLMessage(ACLMessage.REQUEST);
            query.setConversationId("convo-stock-query");
            for (AID market : _deliveryAgent.get_markets()) {
                query.addReceiver(market);
            }
            Util.log(_deliveryAgent, "Sending stock queries to markets...");
            _deliveryAgent.send(query);
            queriesSent = true;
        }

        ACLMessage msg;
        while (marketStocks.size() < _deliveryAgent.get_markets().size() && (msg = _deliveryAgent.blockingReceive(MessageTemplate.MatchConversationId("convo-stock-query"))) != null) {
            Map<String, Double> stock = parseStock(msg.getContent());
            marketStocks.put(msg.getSender(), stock);
            Util.log(_deliveryAgent, "<-\t[" + msg.getSender().getLocalName() + "] Received stock from market");
        }
        block(500);
    }

    private Map<String, Double> parseStock(String content) {
        Map<String, Double> stock = new HashMap<>();
        if (content == null || content.isEmpty()) {
            return stock;
        }

        String[] items = content.split(",");
        for (String itemEntry : items) {
            String[] parts = itemEntry.split(":");
            if (parts.length == 2) {
                String item = parts[0].trim();
                try {
                    double price = Double.parseDouble(parts[1].trim().replace(",", "."));
                    stock.put(item, price);
                } catch (Exception e) {
                    Util.log(_deliveryAgent, "Error parsing item: " + itemEntry + " <" + parts[1] + ">");
                }
            }
        }

        return stock;
    }

    @Override
    public boolean done() {
        long timeout = 4000;
        return (System.currentTimeMillis() - startTime > timeout) || (marketStocks.size() == _deliveryAgent.get_markets().size());
    }

    @Override
    public int onEnd() {
        List<String> remainingItems = new ArrayList<>(_deliveryAgent.get_order());
        double totalCost = 0.0;

        while (!remainingItems.isEmpty()) {
            AID selectedMarket = null;
            List<String> selectedItems = new ArrayList<>();
            int maxItemsCount = 0;
            double selectedMarketCost = 0.0;

            for (Map.Entry<AID, Map<String, Double>> entry : marketStocks.entrySet()) {
                AID market = entry.getKey();
                Map<String, Double> stock = entry.getValue();
                List<String> avaiableItems = new ArrayList<>();
                double costForMarket = 0.0;
                for (String item : remainingItems) {
                    if (stock.containsKey(item)) {
                        avaiableItems.add(item);
                        costForMarket += stock.get(item);
                    }
                }

                int availableCount = avaiableItems.size();
                if (availableCount > maxItemsCount) {
                    maxItemsCount = availableCount;
                    selectedMarket = market;
                    selectedItems = avaiableItems;
                    selectedMarketCost = costForMarket;
                } else if (availableCount == maxItemsCount && availableCount > 0) {
                    if (costForMarket < selectedMarketCost) {
                        selectedMarket = market;
                        selectedItems = avaiableItems;
                        selectedMarketCost = costForMarket;
                    }
                }
            }

            if (selectedMarket == null || selectedItems.isEmpty()) {
                Util.log(_deliveryAgent, "Unable to find the remaining items: " + remainingItems);
                break;
            }

            remainingItems.removeAll(selectedItems);
            totalCost += selectedMarketCost;
            Util.log(_deliveryAgent, "Selected market " + selectedMarket.getLocalName() + " for items " + selectedItems + " with cost " + String.format(Locale.US, "%.2f", selectedMarketCost));
        }

        totalCost += _deliveryAgent.get_deliveryFee();
        Util.log(_deliveryAgent, "Total order cost: " + String.format(Locale.US, "%.2f", totalCost));

        ACLMessage reply = new ACLMessage(ACLMessage.INFORM);
        reply.addReceiver(_deliveryAgent.get_client());
        reply.setConversationId("convo-order");
        reply.setContent(String.format(Locale.US, "%.2f", totalCost));

        Util.log(_deliveryAgent, "Sending price " + String.format(Locale.US, "%.2f", totalCost));
        _deliveryAgent.send(reply);

        return super.onEnd();
    }
}
