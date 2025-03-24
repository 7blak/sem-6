package org.behaviours.market;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import jade.domain.FIPAAgentManagement.FailureException;
import jade.lang.acl.ACLMessage;
import jade.lang.acl.MessageTemplate;
import jade.proto.ContractNetResponder;
import org.agents.MarketAgent;

import java.util.ArrayList;
import java.util.List;

public class SellProductsBehaviour extends ContractNetResponder {
    private final MarketAgent _marketAgent;
    private static final ObjectMapper MAPPER = new ObjectMapper();

    public SellProductsBehaviour(MarketAgent marketAgent) {
        super(marketAgent, MessageTemplate.MatchPerformative(ACLMessage.CFP));
        _marketAgent = marketAgent;
    }

    @Override
    protected ACLMessage handleCfp(ACLMessage cfp) {
        List<String> requestedItems;
        List<String> offeredItems = new ArrayList<>();
        try {
            //noinspection unchecked
            requestedItems = MAPPER.readValue(cfp.getContent(), List.class);
        } catch (JsonProcessingException e) {
            throw new RuntimeException(e);
        }

        System.out.printf("[%s] Received cfp: %s\n", _marketAgent.getLocalName(), requestedItems);
        double totalCost = 0.0;
        for (String item : requestedItems) {
            if (_marketAgent.get_stock().containsKey(item)) {
                offeredItems.add(item);
                totalCost += _marketAgent.get_stock().get(item);
            }
        }

        ACLMessage reply = cfp.createReply();
        if (offeredItems.isEmpty()) {
            reply.setPerformative(ACLMessage.REFUSE);
            reply.setContent("[" + _marketAgent.getLocalName() + "] No items avaiable");
        } else {
            reply.setPerformative(ACLMessage.PROPOSE);
            reply.setContent(String.join(",", offeredItems) + ":" + totalCost);
        }
        return reply;
    }

    @Override
    protected ACLMessage handleAcceptProposal(ACLMessage cfp, ACLMessage propose, ACLMessage accept) throws FailureException {
        return super.handleAcceptProposal(cfp, propose, accept);
        // Currently unused, code here executes when DeliveryAgent accepts this Market's offering.
    }

    @Override
    protected void handleRejectProposal(ACLMessage cfp, ACLMessage propose, ACLMessage reject) {
        super.handleRejectProposal(cfp, propose, reject);
        // Currently unused, code here executes when DeliveryAgent refuses this Market's offering.
    }
}
