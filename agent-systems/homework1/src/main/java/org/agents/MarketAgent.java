package org.agents;

import jade.core.Agent;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.*;
import jade.domain.FIPAException;
import lombok.Getter;
import org.Util;
import org.behaviours.market.SellItemsBehaviour;
import org.exceptions.InvalidServiceSpecification;

import java.util.Map;

@Getter
public class MarketAgent extends Agent {
    private Map<String, Double> _stock;

    @Override
    protected void setup() {
        final Object[] args = getArguments();
        //noinspection unchecked
        _stock = (Map<String, Double>) args[0];

        Util.log(this, "Open for business! Current stock is: " + _stock.toString());
        registerMarketService();

        addBehaviour(new SellItemsBehaviour(this));
    }

    private void registerMarketService() {
        final ServiceDescription sd = new ServiceDescription();
        sd.setType("market");
        sd.setName(getLocalName());
        sd.setOwnership(getLocalName());

        try {
            final DFAgentDescription dfd = new DFAgentDescription();
            dfd.addServices(sd);
            DFService.register(this, dfd);
        } catch (FIPAException e) {
            throw new InvalidServiceSpecification(e);
        }
    }
}
