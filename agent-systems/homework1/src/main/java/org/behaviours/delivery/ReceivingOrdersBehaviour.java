package org.behaviours.delivery;

import jade.core.AID;
import jade.core.behaviours.CyclicBehaviour;
import jade.lang.acl.ACLMessage;
import jade.lang.acl.MessageTemplate;
import org.Util;
import org.agents.DeliveryAgent;

import java.util.Arrays;
import java.util.List;

public class ReceivingOrdersBehaviour extends CyclicBehaviour {
    private final DeliveryAgent _deliveryAgent;

    public ReceivingOrdersBehaviour(DeliveryAgent deliveryAgent) {
        super(deliveryAgent);
        _deliveryAgent = deliveryAgent;
    }

    @Override
    public void action() {
        MessageTemplate mt = new MessageTemplate((MessageTemplate.MatchExpression) msg -> msg.getConversationId() != null && msg.getConversationId().startsWith("order:"));

        ACLMessage msg = _deliveryAgent.receive(mt);
        if (msg != null) {
            Util.log(_deliveryAgent, "-> [" + msg.getSender().getLocalName() + "] Received order: " + msg.getContent());
            List<String> _orderItems = Arrays.asList(msg.getContent().split(","));
            AID _clientAID = msg.getSender();
            _deliveryAgent.addBehaviour(new ClientHandlerBehaviour(_deliveryAgent, _clientAID, _orderItems, msg.getConversationId().replace("order:", "")));
        } else {
            block();
        }
    }
}
