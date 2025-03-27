package org.agents;

import jade.core.AID;
import jade.core.Agent;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import jade.domain.FIPAException;
import lombok.Getter;
import lombok.Setter;
import org.Engine;
import org.Util;
import org.behaviours.delivery.SearchMarketBehaviour;
import org.exceptions.InvalidServiceSpecification;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.CountDownLatch;

@Getter
public class DeliveryAgent extends Agent {
    private Double _deliveryFee;
    @Setter
    private List<AID> _markets;
    public static CountDownLatch _latch = new CountDownLatch(Engine.deliveryAgentNumber);

    @Override
    protected void setup() {
        final Object[] args = getArguments();
        _deliveryFee = (Double) args[0];
        _markets = new ArrayList<>();

        Util.log(this, "Ready to deliver! My delivery fee is: " + _deliveryFee);
        registerDeliveryService();


        while (true) {
            try {
                MarketAgent._latch.await();
                break;
            } catch (InterruptedException ignored) {}
        }
        addBehaviour(new SearchMarketBehaviour(this));
    }

    private void registerDeliveryService() {
        final ServiceDescription sd = new ServiceDescription();
        sd.setType("delivery");
        sd.setName(getLocalName());
        sd.setOwnership(getLocalName());

        try {
            final DFAgentDescription dfd = new DFAgentDescription();
            dfd.addServices(sd);
            DFService.register(this, dfd);

        } catch (final FIPAException e) {
            throw new InvalidServiceSpecification(e);
        }
    }
}
