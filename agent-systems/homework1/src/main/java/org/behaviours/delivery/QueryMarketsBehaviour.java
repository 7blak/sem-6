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
    private final long _startTime;
    private final Map<AID, Map<String, Double>> _marketStocks = new HashMap<>();
    private final AID _clientAID;
    private final List<String> _clientOrder;
    private boolean _queriesSent = false;

    public QueryMarketsBehaviour(DeliveryAgent deliveryAgent, AID clientAID, List<String> clientOrder) {
        super(deliveryAgent);
        _deliveryAgent = deliveryAgent;
        _startTime = System.currentTimeMillis();
        _clientAID = clientAID;
        _clientOrder = clientOrder;
    }

    @Override
    public void action() {
        if (!_queriesSent) {
            ACLMessage query = new ACLMessage(ACLMessage.REQUEST);
            query.setConversationId(String.format("convo-stock-query-%s", _deliveryAgent.getLocalName()));
            for (AID market : _deliveryAgent.get_markets()) {
                query.addReceiver(market);
            }
            Util.log(_deliveryAgent, "Sending stock queries to markets...");
            _deliveryAgent.send(query);
            _queriesSent = true;
        }

        ACLMessage msg;
        while (_marketStocks.size() < _deliveryAgent.get_markets().size() && (msg = _deliveryAgent.blockingReceive(MessageTemplate.MatchConversationId(String.format("convo-stock-query-%s", _deliveryAgent.getLocalName())))) != null) {
            Map<String, Double> stock = parseStock(msg.getContent());
            _marketStocks.put(msg.getSender(), stock);
            Util.log(_deliveryAgent, "<- [" + msg.getSender().getLocalName() + "] Received stock from market");
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
        return (System.currentTimeMillis() - _startTime > timeout) || (_marketStocks.size() == _deliveryAgent.get_markets().size());
    }

    @Override
    public int onEnd() {
        List<String> remainingItems = new ArrayList<>(_clientOrder);
        double totalCost = 0.0;

        while (!remainingItems.isEmpty()) {
            AID selectedMarket = null;
            List<String> selectedItems = new ArrayList<>();
            int maxItemsCount = 0;
            double selectedMarketCost = 0.0;

            for (Map.Entry<AID, Map<String, Double>> entry : _marketStocks.entrySet()) {
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
        reply.addReceiver(_clientAID);
        reply.setConversationId(String.format("convo-order-%s", _clientAID.getLocalName()));
        reply.setContent(String.format(Locale.US, "%.2f", totalCost));

        Util.log(_deliveryAgent, "Sending price " + String.format(Locale.US, "%.2f", totalCost));
        _deliveryAgent.send(reply);

        return super.onEnd();
    }
}
