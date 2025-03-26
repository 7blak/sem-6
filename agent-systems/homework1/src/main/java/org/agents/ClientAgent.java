package org.agents;

import jade.core.AID;
import jade.core.Agent;
import lombok.Getter;
import lombok.Setter;
import org.Util;
import org.behaviours.client.SearchDeliveryBehaviour;

import java.util.ArrayList;
import java.util.List;

@Getter
public class ClientAgent extends Agent {
    private List<String> _order;
    @Setter
    private List<AID> _delivery;

    @SuppressWarnings("unchecked")
    @Override
    protected void setup() {
        final Object[] args = getArguments();
        _order = (List<String>) args[0];
        _delivery = new ArrayList<>();

        Util.log(this, "Ready to order! My order list is: " + _order.toString());

        while(true) {
            try {
                DeliveryAgent._latch.await();
                break;
            } catch (InterruptedException ignored) {}
        }
        addBehaviour(new SearchDeliveryBehaviour(this));
    }
}
