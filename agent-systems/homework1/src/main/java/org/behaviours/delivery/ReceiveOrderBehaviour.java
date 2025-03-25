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
        if (msg != null && "convo-order".equals(msg.getConversationId())) {
            Util.log(_deliveryAgent, "Received order: " + msg.getContent());

            _deliveryAgent.set_client(msg.getSender());
            List<String> orderItems = Arrays.asList(msg.getContent().split(","));
            _deliveryAgent.set_order(orderItems);

            _deliveryAgent.addBehaviour(new QueryMarketsBehaviour(_deliveryAgent));
        } else {
            block();
        }
    }
}
