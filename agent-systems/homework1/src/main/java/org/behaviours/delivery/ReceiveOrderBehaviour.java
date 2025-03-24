package org.behaviours.delivery;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import jade.lang.acl.ACLMessage;
import jade.lang.acl.MessageTemplate;
import jade.proto.ContractNetResponder;
import org.agents.DeliveryAgent;

import java.util.List;
import java.util.Random;

public class ReceiveOrderBehaviour extends ContractNetResponder {
    private final DeliveryAgent _deliveryAgent;
    private static final ObjectMapper MAPPER = new ObjectMapper();

    public ReceiveOrderBehaviour(DeliveryAgent deliveryAgent) {
        super(deliveryAgent, MessageTemplate.MatchPerformative(ACLMessage.CFP));
        _deliveryAgent = deliveryAgent;
    }

    @Override
    protected ACLMessage handleCfp(ACLMessage cfp) {
        List<String> clientOrder;
        try {
            //noinspection unchecked
            clientOrder = MAPPER.readValue(cfp.getContent(), List.class);
        } catch (JsonProcessingException e) {
            throw new RuntimeException(e);
        }
        System.out.printf("[%s] Received cfp: %s\n", _deliveryAgent.getLocalName(), clientOrder);
        _deliveryAgent.set_order(clientOrder);

        _deliveryAgent.addBehaviour(new QueryMarketsBehaviour(_deliveryAgent));

        Random rand = new Random();
        final double finalCost = rand.nextDouble() * 50;

        ACLMessage reply = cfp.createReply();
        reply.setPerformative(ACLMessage.PROPOSE);
        reply.setContent(Double.toString(finalCost));
        System.out.printf("[%s] Sent reply: %s\n", _deliveryAgent.getLocalName(), reply.getContent());
        return reply;
    }

    @Override
    protected ACLMessage handleAcceptProposal(ACLMessage cfp, ACLMessage propose, ACLMessage accept) {
        System.out.printf("[%s] Received accept proposal from %s\n", _deliveryAgent.getLocalName(), accept.getSender());
        ACLMessage reply = accept.createReply();
        reply.setPerformative(ACLMessage.INFORM);
        reply.setContent("[" + _deliveryAgent.getLocalName() + "]: Conirms delivery, processing order.");
        return reply;
    }

    @Override
    protected void handleRejectProposal(ACLMessage cfp, ACLMessage propose, ACLMessage reject) {
        System.out.printf("[%s] Received reject proposal from %s\n", _deliveryAgent.getLocalName(), reject.getSender());
    }
}
