package org.behaviours.market;

import jade.core.behaviours.CyclicBehaviour;
import jade.lang.acl.ACLMessage;
import org.Util;
import org.agents.MarketAgent;

import java.util.Locale;
import java.util.Map;

public class SellItemsBehaviour extends CyclicBehaviour {
    private final MarketAgent _marketAgent;

    public SellItemsBehaviour(MarketAgent marketAgent) {
        super(marketAgent);
        _marketAgent = marketAgent;
    }

    @Override
    public void action() {
        ACLMessage msg = _marketAgent.receive();
        if (msg != null && String.format("convo-stock-query-%s", msg.getSender().getLocalName()).equals(msg.getConversationId())) {
            ACLMessage reply = msg.createReply();
            reply.setPerformative(ACLMessage.INFORM);
            StringBuilder stockContent = new StringBuilder();

            for (Map.Entry<String, Double> entry : _marketAgent.get_stock().entrySet()) {
                if (!stockContent.isEmpty()) {
                    stockContent.append(",");
                }

                stockContent.append(entry.getKey()).append(":").append(String.format(Locale.US, "%.2f", entry.getValue()));
            }

            reply.setContent(stockContent.toString());
            reply.setConversationId(String.format("convo-stock-query-%s", msg.getSender().getLocalName()));
            Util.log(_marketAgent, "->\t[" + msg.getSender().getLocalName() + "] Replied with stock: " + stockContent);
            _marketAgent.send(reply);
        } else {
            block();
        }
    }
}
