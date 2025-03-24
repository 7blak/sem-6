package org.behaviours.client;

import com.fasterxml.jackson.databind.ObjectMapper;
import jade.lang.acl.ACLMessage;
import jade.proto.ContractNetInitiator;
import org.agents.ClientAgent;
import org.exceptions.InvalidMessageContentException;

import java.util.Vector;

public class MakeOrderBehaviour extends ContractNetInitiator {
    private final ClientAgent _clientAgent;
    private static final ObjectMapper MAPPER = new ObjectMapper();

    public MakeOrderBehaviour(ClientAgent clientAgent) {
        super(clientAgent, orderMessage(clientAgent));
        _clientAgent = clientAgent;
    }

    private static ACLMessage orderMessage(ClientAgent clientAgent) {
        try {
            final ACLMessage cfp = new ACLMessage(ACLMessage.CFP);
            cfp.setContent(MAPPER.writeValueAsString(clientAgent.get_order()));
            clientAgent.get_delivery().forEach(cfp::addReceiver);
            System.out.printf("[%s] Sending order %s\n", clientAgent.getLocalName(), cfp.getContent());
            return cfp;
        } catch (final Exception e) {
            throw new InvalidMessageContentException(e);
        }
    }

    @Override
    protected void handlePropose(ACLMessage propose, Vector acceptances) {
        System.out.printf("[%s] Received proposal from %s: %s\n", _clientAgent.getLocalName(), propose.getSender().getLocalName(), propose.getContent());
    }

    @Override
    protected void handleRefuse(ACLMessage refuse) {
        System.out.printf("[%s] Received refusal from %s: %s\n", _clientAgent.getLocalName(), refuse.getSender().getLocalName(), refuse.getContent());
    }

    @Override
    protected void handleFailure(ACLMessage failure) {
        if (failure.getSender().equals(_clientAgent.getAMS())) {
            System.out.printf("[%s] Responder does not exist\n", _clientAgent.getLocalName());
        } else {
            System.out.printf("[%s] Agent %s has failed\n", _clientAgent.getLocalName(), failure.getSender().getLocalName());
        }
    }

    @Override
    protected void handleInform(ACLMessage inform) {
        super.handleInform(inform);
    }

    @Override
    protected void handleAllResponses(Vector responses, Vector acceptances) {
        ACLMessage bestProposal = null;
        double bestCost = Double.MAX_VALUE;
        for (Object resp : responses) {
            ACLMessage msg = (ACLMessage) resp;
            if (msg.getPerformative() == ACLMessage.PROPOSE) {
                double cost = Double.parseDouble(msg.getContent());
                if (cost < bestCost) {
                    bestCost = cost;
                    bestProposal = msg;
                }
            }
        }

        for (Object resp : responses) {
            ACLMessage msg = (ACLMessage) resp;
            ACLMessage reply = msg.createReply();
            if (msg.equals(bestProposal)) {
                reply.setPerformative(ACLMessage.ACCEPT_PROPOSAL);
            } else {
                reply.setPerformative(ACLMessage.REJECT_PROPOSAL);
            }
            //noinspection unchecked
            acceptances.add(reply);
        }
    }
}
