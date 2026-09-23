package com.clearmeasure.workorders.web;

import com.microsoft.playwright.*;
import com.microsoft.playwright.options.AriaRole;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import static org.junit.jupiter.api.Assertions.*;

/** Real Chromium acceptance run. Video is written as a build artifact for PR review. */
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
class WorkOrderBrowserAcceptanceTest {
    @LocalServerPort int port;

    @Test void browserCreatesAndAssignsWorkOrderWhileRecordingVideo() throws Exception {
        Path videoDir = Path.of("target", "playwright-recordings");
        Files.createDirectories(videoDir);
        boolean headful = Boolean.parseBoolean(System.getenv().getOrDefault("PLAYWRIGHT_HEADFUL", "false"));
        Path capturedVideo;
        try (Playwright playwright = Playwright.create()) {
            Browser browser = playwright.chromium().launch(new BrowserType.LaunchOptions().setHeadless(!headful));
            BrowserContext context = browser.newContext(new Browser.NewContextOptions()
                .setRecordVideoDir(videoDir).setRecordVideoSize(1280, 720));
            Page page = context.newPage();
            Video video = page.video();
            page.navigate("http://localhost:" + port + "/");
            page.getByLabel("Title").fill("Acceptance repair");
            page.getByLabel("Room").fill("A-17");
            page.getByLabel("Creator").fill("Acceptance tester");
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Save draft")).click();
            assertTrue(page.getByText("Acceptance repair").isVisible());
            page.getByPlaceholder("Assignee").fill("Facilities team");
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Assign")).click();
            assertTrue(page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair")).getByText("ASSIGNED").isVisible());
            context.close(); // flushes the recorded webm to disk
            capturedVideo = video.path();
            browser.close();
        }
        Files.copy(capturedVideo, videoDir.resolve("acceptance.webm"), StandardCopyOption.REPLACE_EXISTING);
        assertTrue(Files.size(videoDir.resolve("acceptance.webm")) > 0, "Browser video should be captured");
    }
}
