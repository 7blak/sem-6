package org.behaviours.client;

import jade.core.AID;
import jade.core.behaviours.CyclicBehaviour;
import jade.lang.acl.ACLMessage;
import org.Engine;
import org.Util;
import org.agents.ClientAgent;

import java.util.HashMap;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.CountDownLatch;

public class MessageReceiverBehaviour extends CyclicBehaviour {
    private final ClientAgent _clientAgent;
    private final Map<AID, Double> _offers = new HashMap<>();
    private int _receivedMessages = 0;
    private static final CountDownLatch _latch = new CountDownLatch(Engine.clientAgentNumber);
    private static final CountDownLatch _finalLatch = new CountDownLatch(Engine.clientAgentNumber);

    public MessageReceiverBehaviour(ClientAgent clientAgent) {
        super(clientAgent);
        _clientAgent = clientAgent;
    }

    @Override
    public void action() {
        if (_receivedMessages >= _clientAgent.get_delivery().size() && _clientAgent.is_offerNotSelected()) {
            _clientAgent.set_offerNotSelected(false);
            SelectBestOffer();
        }

        ACLMessage msg = _clientAgent.receive();
        if (msg != null) {
            String convoId = msg.getConversationId();
            if (convoId != null && convoId.startsWith("order-price:")) {
                double price = Double.parseDouble(msg.getContent());
                _offers.put(msg.getSender(), price);
                Util.log(_clientAgent, "Received offer from " + msg.getSender().getLocalName() + ": " + String.format(Locale.US, "%.2f", price));
                _receivedMessages++;

            } else if (convoId != null && convoId.startsWith("delivery-confirm:")) {
                if (_finalLatch.getCount() == 1) {
                    System.out.println("-------ALL CLIENTS ARE RECEIVING THEIR ORDERS!!!-------");
                }
                _finalLatch.countDown();
                while (true) {
                    try {
                        _finalLatch.await();
                        break;
                    } catch (InterruptedException ignored) {}
                }

                Util.log(_clientAgent, "Got the order! Deliverer says: " + msg.getContent());

            } else block();
        } else {
            block();
        }
    }

    private void SelectBestOffer() {
        if (_offers.isEmpty()) {
            Util.log(_clientAgent, "No offers received");
        } else {
            AID bestDelivery = null;
            double bestPrice = Double.MAX_VALUE;
            for (var entry : _offers.entrySet()) {
                if (entry.getValue() < bestPrice) {
                    bestPrice = entry.getValue();
                    bestDelivery = entry.getKey();
                }
            }
            if (bestDelivery != null) {
                if (_latch.getCount() == 1)
                    System.out.println("-----RESULTS-----");

                _latch.countDown();
                while (true) {
                    try {
                        _latch.await();
                        break;
                    } catch (InterruptedException ignored) {}
                }
                Util.log(_clientAgent, "Selected delivery agent: " + bestDelivery.getLocalName() + " with price " + String.format(Locale.US, "%.2f (Received offers was: %d)", bestPrice, _receivedMessages));

                for (var delivery : _clientAgent.get_delivery()) {
                    String convoId = _clientAgent.get_orderConvoIds().get(delivery);
                    ACLMessage deliveryConfirm = new ACLMessage(ACLMessage.INFORM);
                    deliveryConfirm.addReceiver(delivery);
                    deliveryConfirm.setConversationId(convoId.replace("order:", "delivery-confirm:"));
                    deliveryConfirm.setContent(delivery.equals(bestDelivery) ? "message-delivery-confirm" : "message-delivery-reject");

                    _clientAgent.send(deliveryConfirm);
                }
            }
        }
    }
}
