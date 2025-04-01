package org.behaviours.delivery;

import jade.core.AID;
import jade.core.behaviours.CyclicBehaviour;
import jade.lang.acl.ACLMessage;
import jade.lang.acl.MessageTemplate;
import org.Util;
import org.agents.DeliveryAgent;

import java.util.*;

public class ClientHandlerBehaviour extends CyclicBehaviour {
    private final DeliveryAgent _deliveryAgent;
    private final Map<AID, Map<String, Double>> _marketStocks = new HashMap<>();
    private final List<AID> _selectedMarkets = new ArrayList<>();
    private boolean _isFinalizing = false;
    private boolean _hasCalculatedCost = false;
    private final AID _clientAID;
    private final List<String> _orderItems;
    private int _marketSellResponses = 0;
    private int _marketQueryResponses = 0;
    private final String _orderConvoId;
    private final Map<AID, List<String>> _marketItems = new HashMap<>();

    public ClientHandlerBehaviour(DeliveryAgent deliveryAgent, AID clientAID, List<String> orderItems, String orderConvoId) {
        super(deliveryAgent);
        _deliveryAgent = deliveryAgent;
        _clientAID = clientAID;
        _orderItems = orderItems;
        _orderConvoId = orderConvoId;

        ACLMessage query = new ACLMessage(ACLMessage.REQUEST);
        query.setConversationId(String.format("stock-query:%s", _orderConvoId));
        for (AID market : _deliveryAgent.get_markets()) {
            query.addReceiver(market);
        }
        Util.log(_deliveryAgent, "Sending stock queries to markets...");
        _deliveryAgent.send(query);
    }

    @Override
    public void action() {
        if (_marketSellResponses >= _selectedMarkets.size() && !_isFinalizing && _hasCalculatedCost) {
            _isFinalizing = true;
            ACLMessage reply = new ACLMessage(ACLMessage.INFORM);
            reply.addReceiver(_clientAID);
            reply.setConversationId(String.format("delivery-confirm:%s", _orderConvoId));
            reply.setContent("Here is your delivery, enjoy!");

            _deliveryAgent.send(reply);
        }

        if (_marketQueryResponses >= _deliveryAgent.get_markets().size() && !_hasCalculatedCost) {
            _hasCalculatedCost = true;
            CalculateCosts();
        }

        MessageTemplate mt = new MessageTemplate((MessageTemplate.MatchExpression) msg -> {
            String cid = msg.getConversationId();
            return cid != null && (
                    cid.equals("stock-query:" + _orderConvoId) ||
                            cid.equals("delivery-confirm:" + _orderConvoId) ||
                            cid.equals("market-buy:" + _orderConvoId)
            );
        });

        ACLMessage msg = _deliveryAgent.receive(mt);

        if (msg != null) {
            String cid = msg.getConversationId();
            if (cid.equals("stock-query:" + _orderConvoId)) {
                Map<String, Double> stock = parseStock(msg.getContent());
                _marketStocks.put(msg.getSender(), stock);
                Util.log(_deliveryAgent, "<- [" + msg.getSender().getLocalName() + "] Received stock from market");
                _marketQueryResponses++;
            } else if (cid.equals("delivery-confirm:" + _orderConvoId)) {
                Util.log(_deliveryAgent, "Received message from " + msg.getSender().getLocalName() + ": " + (msg.getContent().equals("message-delivery-confirm") ? "I want to buy from you!" : "I do not want what you are selling"));
                if (msg.getContent().equals("message-delivery-confirm")) {
                    Util.log(_deliveryAgent, "Buying needed items from markets...");
                    buyItemsFromMarket();
                }
            } else if (cid.equals("market-buy:" + _orderConvoId)) {
                Util.log(_deliveryAgent, "Received items from market " + msg.getSender().getLocalName());
                _marketSellResponses++;
            }
        } else {
            block();
        }
    }

    private void CalculateCosts() {
        List<String> remainingItems = new ArrayList<>(_orderItems);
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
            _selectedMarkets.add(selectedMarket);
            _marketItems.put(selectedMarket, selectedItems);
            Util.log(_deliveryAgent, "Selected market " + selectedMarket.getLocalName() + " for items " + selectedItems + " with cost " + String.format(Locale.US, "%.2f", selectedMarketCost));
        }

        totalCost += _deliveryAgent.get_deliveryFee();
        Util.log(_deliveryAgent, "Total order cost: " + String.format(Locale.US, "%.2f", totalCost));

        ACLMessage reply = new ACLMessage(ACLMessage.INFORM);
        reply.addReceiver(_clientAID);
        reply.setConversationId(String.format("order-price:%s", _orderConvoId));
        reply.setContent(String.format(Locale.US, "%.2f", totalCost));

        Util.log(_deliveryAgent, "Sending price " + String.format(Locale.US, "%.2f", totalCost));
        _deliveryAgent.send(reply);
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

    private void buyItemsFromMarket() {
        for (var selectedMarket : _selectedMarkets) {
            ACLMessage buyMarketMessage = new ACLMessage(ACLMessage.REQUEST);
            buyMarketMessage.addReceiver(selectedMarket);
            buyMarketMessage.setConversationId(String.format("market-buy:%s", _orderConvoId));
            buyMarketMessage.setContent(String.format("[%s] Buying needed items from order: %s", _deliveryAgent.getLocalName(), _marketItems.get(selectedMarket).toString()));

            _deliveryAgent.send(buyMarketMessage);
        }
    }
}
