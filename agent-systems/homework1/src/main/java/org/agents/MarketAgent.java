package org.agents;

import jade.core.Agent;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import jade.domain.FIPAException;
import org.exceptions.InvalidServiceSpecification;

import java.util.Map;

public class MarketAgent extends Agent {
    private Map<String, Double> _stock;

    @SuppressWarnings("unchecked")
    @Override
    protected void setup() {
        final Object[] args = getArguments();
        _stock = (Map<String, Double>) args[0];

        System.out.printf("[%s} Open for business! Current stock is: %s\n", getLocalName(), _stock.toString());
        registerMarketService();
    }

    private void registerMarketService() {
        final ServiceDescription sd = new ServiceDescription();
        sd.setType("market");
        sd.setName("Market");
        sd.setOwnership(this.getLocalName());

        try {
            final DFAgentDescription dfd = new DFAgentDescription();
            dfd.addServices(sd);
            DFService.register(this, dfd);
        } catch (FIPAException e) {
            throw new InvalidServiceSpecification(e);
        }
    }
}
