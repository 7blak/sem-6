package org.behaviours.client;

import jade.core.behaviours.OneShotBehaviour;
import jade.lang.acl.ACLMessage;
import org.Util;
import org.agents.ClientAgent;

import java.util.UUID;

public class SendOrderBehaviour extends OneShotBehaviour {
    private final ClientAgent _clientAgent;

    public SendOrderBehaviour(ClientAgent clientAgent) {
        super(clientAgent);
        _clientAgent = clientAgent;
    }

    @Override
    public void action() {
        if (_clientAgent.get_delivery().isEmpty()) {
            Util.log(_clientAgent, "No delivery found");
            return;
        }

        String orderContent = String.join(",", _clientAgent.get_order());

        for (var delivery : _clientAgent.get_delivery()) {
            ACLMessage orderMsg = new ACLMessage(ACLMessage.REQUEST);
            orderMsg.setContent(orderContent);
            orderMsg.addReceiver(delivery);
            String token = UUID.randomUUID().toString();
            orderMsg.setConversationId(String.format("order:%s-%s:%s", _clientAgent.getLocalName(), delivery.getLocalName(), token));
            _clientAgent.get_orderConvoIds().put(delivery, String.format("order:%s-%s:%s", _clientAgent.getLocalName(), delivery.getLocalName(), token));

            Util.log(_clientAgent, "Order sent to [" + delivery.getLocalName() + "]");
            _clientAgent.send(orderMsg);
        }
    }
}
