package org.behaviours.client;

import jade.core.behaviours.OneShotBehaviour;
import jade.lang.acl.ACLMessage;
import org.Util;
import org.agents.ClientAgent;

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
            orderMsg.setConversationId(String.format("convo-order-%s", _clientAgent.getLocalName()));

            Util.log(_clientAgent, "Order sent to [" + delivery.getLocalName() + "]");
            _clientAgent.send(orderMsg);
        }
    }
}
