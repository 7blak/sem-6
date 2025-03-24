package org.behaviours.delivery;

import com.fasterxml.jackson.databind.ObjectMapper;
import jade.lang.acl.ACLMessage;
import jade.proto.ContractNetInitiator;
import org.agents.DeliveryAgent;
import org.exceptions.InvalidMessageContentException;

import java.util.Vector;

public class QueryMarketsBehaviour extends ContractNetInitiator {
    private final DeliveryAgent _deliveryAgent;
    private static final ObjectMapper MAPPER = new ObjectMapper();

    public QueryMarketsBehaviour(DeliveryAgent deliveryAgent) {
        super(deliveryAgent, queryMessage(deliveryAgent));
        _deliveryAgent = deliveryAgent;
    }

    private static ACLMessage queryMessage(DeliveryAgent deliveryAgent) {
        try {
            final ACLMessage cfp = new ACLMessage(ACLMessage.CFP);
            cfp.setContent(MAPPER.writeValueAsString(deliveryAgent.get_order()));
            deliveryAgent.get_markets().forEach(cfp::addReceiver);
            System.out.printf("[%s] Querying markets for: %s\n", deliveryAgent.getLocalName(), cfp.getContent());
            return cfp;
        } catch (final Exception e) {
            throw new InvalidMessageContentException(e);
        }
    }

    @Override
    protected void handlePropose(ACLMessage propose, Vector acceptances) {
        System.out.printf("[%s] Received proposal from %s: %s\n", _deliveryAgent.getLocalName(), propose.getSender().getLocalName(), propose.getContent());
    }

    @Override
    protected void handleRefuse(ACLMessage refuse) {
        System.out.printf("[%s] Received refusal from %s: %s\n", _deliveryAgent.getLocalName(), refuse.getSender().getLocalName(), refuse.getContent());
    }

    @Override
    protected void handleFailure(ACLMessage failure) {
        if (failure.getSender().equals(_deliveryAgent.getAMS())) {
            System.out.printf("[%s] Responder does not exist\n", _deliveryAgent.getLocalName());
        } else {
            System.out.printf("[%s] Agent %s has failed\n", _deliveryAgent.getLocalName(), failure.getSender().getLocalName());
        }
    }

    @Override
    protected void handleInform(ACLMessage inform) {
        super.handleInform(inform);
    }

    @Override
    protected void handleAllResponses(Vector responses, Vector acceptances) {
        System.out.printf("[%s] Received %d responses, processing data...\n", _deliveryAgent.getLocalName(), responses.size());
    }
}
