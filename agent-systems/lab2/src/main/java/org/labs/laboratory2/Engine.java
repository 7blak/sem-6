package org.labs.laboratory2;

import static org.labs.JADEEngine.runAgent;
import static org.labs.JADEEngine.runGUI;
import static org.labs.laboratory2.domain.Genre.COMEDY;
import static org.labs.laboratory2.domain.Genre.CRIMINAL;
import static org.labs.laboratory2.domain.Genre.HORROR;
import static org.labs.laboratory2.domain.Genre.SCIFI;
import static org.labs.laboratory2.domain.Region.EU;
import static org.labs.laboratory2.domain.Region.US;

import java.util.List;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import org.labs.exceptions.JadePlatformInitializationException;

import jade.core.Profile;
import jade.core.ProfileImpl;
import jade.core.Runtime;
import jade.wrapper.ContainerController;

public class Engine {

	private static final ExecutorService jadeExecutor = Executors.newCachedThreadPool();

	public static void main(String[] args) {
		final Runtime runtime = Runtime.instance();
		final Profile profile = new ProfileImpl();

		try {
			final ContainerController container = jadeExecutor.submit(() -> runtime.createMainContainer(profile)).get();

			runGUI(container);
			runPomodoroTask(container);
//			runDFTask(container);
		} catch (final InterruptedException | ExecutionException e) {
			throw new JadePlatformInitializationException(e);
		}
	}

	private static void runPomodoroTask(final ContainerController container) {
		runAgent(container, "Pomodoro", "PomodoroAgent", "laboratory2");
		runAgent(container, "Manager", "StudyManagerAgent", "laboratory2");
	}

	private static void runDFTask(final ContainerController container) {
		runAgent(container, "Hulu", "StreamingPlatformAgent", "laboratory2",
				new Object[] { US, true, List.of(COMEDY, CRIMINAL) });
		runAgent(container, "Peacock", "StreamingPlatformAgent", "laboratory2",
				new Object[] { US, true, List.of(HORROR, CRIMINAL) });
		runAgent(container, "Netflix", "StreamingPlatformAgent", "laboratory2",
				new Object[] { EU, true, List.of(HORROR, COMEDY, CRIMINAL) });
		runAgent(container, "HBO", "StreamingPlatformAgent", "laboratory2",
				new Object[] { EU, false, List.of(COMEDY, SCIFI) });

		runAgent(container, "TypicalViewer", "ViewerAgent", "laboratory2",
				new Object[] { CRIMINAL, EU });
	}
}
