package org.agents;

import jade.core.AID;
import jade.core.Agent;
import jade.domain.DFService;
import jade.domain.FIPAAgentManagement.DFAgentDescription;
import jade.domain.FIPAAgentManagement.ServiceDescription;
import jade.domain.FIPAException;
import lombok.Getter;
import lombok.Setter;
import org.behaviours.delivery.SearchMarketBehaviour;
import org.exceptions.InvalidServiceSpecification;

import java.util.ArrayList;
import java.util.List;

@Getter
public class DeliveryAgent extends Agent {
    private Double _deliveryFee;
    @Setter
    private List<AID> _markets;
    @Setter
    private List<String> _order;
    @Setter
    private AID _client;

    @Override
    protected void setup() {
        final Object[] args = getArguments();
        _deliveryFee = (Double) args[0];
        _markets = new ArrayList<>();
        _order = new ArrayList<>();

        System.out.printf("[%s] Ready to deliver! My delivery fee is: %.2f\n", getLocalName(), _deliveryFee);
        registerDeliveryService();

        addBehaviour(new SearchMarketBehaviour(this, 2000));
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
