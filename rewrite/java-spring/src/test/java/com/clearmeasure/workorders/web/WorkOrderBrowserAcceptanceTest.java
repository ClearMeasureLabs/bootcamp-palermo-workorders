package com.clearmeasure.workorders.web;

import com.microsoft.playwright.*;
import com.microsoft.playwright.options.AriaRole;
import com.microsoft.playwright.options.WaitForSelectorState;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import static org.junit.jupiter.api.Assertions.*;

/** Real Chromium acceptance run. Video is written as a build artifact for PR review. */
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT, properties = {
    "server.error.include-message=always", "server.error.include-exception=true", "server.error.include-stacktrace=always",
    "logging.level.org.springframework.web=DEBUG"
})
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
            page.getByLabel("Instructions").fill("Bring a ladder and lockout kit");
            page.getByLabel("Due date").fill("2026-10-01");
            Response saveResponse = page.waitForResponse(
                response -> response.url().endsWith("/work-orders") && response.request().method().equals("POST"),
                () -> page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Save draft")).click());
            assertEquals(302, saveResponse.status(), "The HTML form should save and redirect to the order list");
            page.waitForURL("**/");
            page.screenshot(new Page.ScreenshotOptions().setPath(videoDir.resolve("after-save.png")));
            String renderedList = page.locator("body").innerText();
            assertTrue(renderedList.contains("Acceptance repair"),
                "The redirect should render the created work order. URL=" + page.url() + " body=" + renderedList);
            String number = page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair"))
                .locator("td").first().innerText();
            APIResponse apiRead = page.request().get("http://localhost:" + port + "/api/work-orders/" + number);
            assertEquals(200, apiRead.status());
            assertTrue(apiRead.text().contains("Bring a ladder and lockout kit"), "Instructions should persist from the browser form");
            assertTrue(renderedList.contains("Due Today") || renderedList.contains("On Track") || renderedList.contains("Overdue"),
                "A dated open order should show an urgency badge");
            page.getByPlaceholder("Assignee").fill("Facilities team");
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Assign")).click();
            page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair")).getByText("ASSIGNED")
                .waitFor(new Locator.WaitForOptions().setState(WaitForSelectorState.VISIBLE));
            context.close(); // flushes the recorded webm to disk
            capturedVideo = video.path();
            browser.close();
        }
        Files.copy(capturedVideo, videoDir.resolve("acceptance.webm"), StandardCopyOption.REPLACE_EXISTING);
        assertTrue(Files.size(videoDir.resolve("acceptance.webm")) > 0, "Browser video should be captured");
    }
}
