package org;

import jade.core.Agent;

import java.util.Locale;

public class Util {
    public static void log(Agent agent, String log) {
        System.out.printf(String.format(Locale.US, "[%s] %s\n", agent.getLocalName(), log));
    }
}
