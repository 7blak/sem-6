package org.agents;

import jade.core.AID;
import jade.core.Agent;
import lombok.Getter;
import lombok.Setter;
import org.Util;
import org.behaviours.client.SearchDeliveryBehaviour;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Getter
public class ClientAgent extends Agent {
    private List<String> _order;
    @Setter
    private List<AID> _delivery;
    @Setter
    private boolean _offerNotSelected = true;
    private final Map<AID, String> _orderConvoIds = new HashMap<>();

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
