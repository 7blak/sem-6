package org.behaviours.client;

import jade.core.AID;
import jade.core.behaviours.Behaviour;
import jade.lang.acl.ACLMessage;
import jade.lang.acl.MessageTemplate;
import org.Engine;
import org.Util;
import org.agents.ClientAgent;

import java.util.HashMap;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.CountDownLatch;

public class SelectOfferBehaviour extends Behaviour {
    private final ClientAgent _clientAgent;
    private final Map<AID, Double> _offers = new HashMap<>();
    private final long _startTime;
    private final long _timeout;
    private int receivedMessages = 0;
    private static final CountDownLatch _latch = new CountDownLatch(Engine.clientAgentNumber);

    public SelectOfferBehaviour(ClientAgent clientAgent, long timeout) {
        super(clientAgent);
        _clientAgent = clientAgent;
        _timeout = timeout;
        _startTime = System.currentTimeMillis();
    }
    @Override
    public void action() {
        ACLMessage msg;
        while (receivedMessages++ < _clientAgent.get_delivery().size() && (msg = _clientAgent.blockingReceive(MessageTemplate.MatchConversationId(String.format("convo-order-%s", _clientAgent.getLocalName())))) != null) {
            double price = Double.parseDouble(msg.getContent());
            _offers.put(msg.getSender(), price);
            Util.log(_clientAgent, "Received offer from " + msg.getSender().getLocalName() + ": " + String.format(Locale.US, "%.2f", price));
        }
        block(500);
    }

    @Override
    public boolean done() {
        return _offers.size() == _clientAgent.get_delivery().size() || System.currentTimeMillis() - _startTime > _timeout;
    }

    @Override
    public int onEnd() {
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

                //noinspection LoopStatementThatDoesntLoop
                while (true) {
                    try {
                        _latch.await();
                        break;
                    } catch (InterruptedException e) {
                        throw new RuntimeException(e);
                    }
                }
                Util.log(_clientAgent, "Selected delivery agent: " + bestDelivery.getLocalName() + " with price " + String.format(Locale.US, "%.2f (Received offers was: %d)", bestPrice, --receivedMessages));
            }
        }

        return super.onEnd();
    }
}
