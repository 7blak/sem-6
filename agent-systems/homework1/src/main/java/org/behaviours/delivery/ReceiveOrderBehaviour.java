package org.behaviours.delivery;

import jade.core.behaviours.CyclicBehaviour;
import jade.lang.acl.ACLMessage;
import org.Util;
import org.agents.DeliveryAgent;

import java.util.Arrays;
import java.util.List;

public class ReceiveOrderBehaviour extends CyclicBehaviour {
    private final DeliveryAgent _deliveryAgent;

    public ReceiveOrderBehaviour(DeliveryAgent deliveryAgent) {
        super(deliveryAgent);
        _deliveryAgent = deliveryAgent;
    }

    @Override
    public void action() {
        ACLMessage msg = _deliveryAgent.receive();
        if (msg != null && String.format("convo-order-%s", msg.getSender().getLocalName()).equals(msg.getConversationId())) {
            Util.log(_deliveryAgent, "-> [" + msg.getSender().getLocalName() + "] Received order: " + msg.getContent());

            List<String> orderItems = Arrays.asList(msg.getContent().split(","));

            _deliveryAgent.addBehaviour(new QueryMarketsBehaviour(_deliveryAgent, msg.getSender(), orderItems));
        } else {
            block();
        }
    }
}
