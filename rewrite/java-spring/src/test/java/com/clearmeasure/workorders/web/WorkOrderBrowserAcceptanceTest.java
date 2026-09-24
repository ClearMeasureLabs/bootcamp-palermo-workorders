package com.clearmeasure.workorders.web;

import com.microsoft.playwright.*;
import com.microsoft.playwright.options.AriaRole;
import com.microsoft.playwright.options.WaitForSelectorState;
import com.microsoft.playwright.options.RequestOptions;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.time.LocalDate;
import java.time.ZoneId;
import static org.junit.jupiter.api.Assertions.*;

/** Real Chromium acceptance run. Video is written as a build artifact for PR review. */
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT, properties = {
    "server.error.include-message=always", "server.error.include-exception=true", "server.error.include-stacktrace=always",
    "logging.level.org.springframework.web=DEBUG"
})
class WorkOrderBrowserAcceptanceTest {
    @LocalServerPort int port;

    @Test void browserUsesDemoSessionAndEnforcesAssignedEmployeeLifecycleWhileRecordingVideo() throws Exception {
        Path videoDir = Path.of("target", "playwright-recordings");
        Files.createDirectories(videoDir);
        boolean headful = Boolean.parseBoolean(System.getenv().getOrDefault("PLAYWRIGHT_HEADFUL", "false"));
        Path capturedVideo;
        try (Playwright playwright = Playwright.create()) {
            Browser browser = playwright.chromium().launch(new BrowserType.LaunchOptions().setHeadless(!headful));
            BrowserContext context = browser.newContext(new Browser.NewContextOptions()
                .setViewportSize(1920, 1080).setRecordVideoDir(videoDir).setRecordVideoSize(1920, 1080));
            Page page = context.newPage();
            Video video = page.video();
            String baseUrl = "http://localhost:" + port;
            page.navigate(baseUrl + "/login");
            assertEquals("HOMER SIMPSON", page.locator("#username option[value='hsimpson']").innerText());
            assertTrue(page.getByTestId("lovejoy-shortcut").isVisible());
            page.locator("#username").selectOption("hsimpson");
            page.getByTestId("login-button").click();
            page.waitForURL("**/");
            assertEquals(baseUrl + "/", page.url(), "Login redirect should not expose a session id in the URL");
            assertTrue(page.getByTestId("welcome-text").innerText().contains("hsimpson"));
            page.getByRole(AriaRole.LINK, new Page.GetByRoleOptions().setName("New Work Order")).click();
            page.waitForURL("**/work-orders/new");
            page.getByLabel("Title").fill("Acceptance repair");
            page.getByLabel("Room").fill("A-17");
            page.getByLabel("Instructions").fill("Bring a ladder and lockout kit");
            page.getByLabel("Due date").fill(LocalDate.now(ZoneId.of("America/Chicago")).plusDays(7).toString());
            Response saveResponse = page.waitForResponse(
                response -> response.url().endsWith("/work-orders") && response.request().method().equals("POST"),
                () -> page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Save draft")).click());
            assertEquals(302, saveResponse.status(), "The HTML form should save and redirect to the order list");
            page.waitForURL("**/");
            page.screenshot(new Page.ScreenshotOptions().setPath(videoDir.resolve("after-save.png")));
            page.screenshot(new Page.ScreenshotOptions().setPath(videoDir.resolve("search-page.png")));
            String renderedList = page.locator("body").innerText();
            assertTrue(renderedList.contains("Acceptance repair"),
                "The redirect should render the created work order. URL=" + page.url() + " body=" + renderedList);
            Locator orderRow = page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair"));
            String number = orderRow.locator("td").first().innerText();
            APIResponse apiRead = page.request().get(baseUrl + "/api/work-orders/" + number);
            assertEquals(200, apiRead.status());
            assertTrue(apiRead.text().contains("Bring a ladder and lockout kit"), "Instructions should persist from the browser form");
            assertTrue(renderedList.contains("Due Today") || renderedList.contains("On Track") || renderedList.contains("Overdue"),
                "A dated open order should show an urgency badge");
            String csrfToken = page.locator("input[name='_csrf']").first().getAttribute("value");
            assertEquals(403, page.request().post(baseUrl + "/api/work-orders/" + number + "/begin").status(),
                "Session cookie alone must not authorize a cross-site mutation");
            orderRow.getByRole(AriaRole.LINK).click();
            page.waitForURL("**/work-orders/*/manage");
            page.screenshot(new Page.ScreenshotOptions().setPath(videoDir.resolve("manage-page.png")));
            assertTrue(page.getByTestId("attachments-section").isVisible());
            assertEquals(0, page.locator("input[type='file']").count(), "This parity slice records metadata and does not upload binary content");
            page.getByLabel("File name").fill("damage-photo.jpg");
            page.getByLabel("Content type").fill("image/jpeg");
            page.getByLabel("File size in bytes").fill("2048");
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Add attachment metadata")).click();
            page.waitForURL("**/work-orders/*/manage");
            assertEquals("damage-photo.jpg", page.getByTestId("attachment-file-name").innerText());
            assertEquals("image/jpeg", page.getByTestId("attachment-content-type").innerText());
            assertEquals("2048", page.getByTestId("attachment-file-size").innerText());
            assertEquals("Homer Simpson", page.getByTestId("attachment-uploaded-by").innerText());
            assertTrue(page.getByTestId("attachment-uploaded-date").innerText().endsWith("Z"),
                "Displayed upload time should be UTC");
            page.navigate(baseUrl + "/");
            assertEquals(403, page.request().post(baseUrl + "/api/work-orders/" + number + "/begin",
                    RequestOptions.create().setHeader("X-CSRF-Token", csrfToken)).status(),
                "The creator cannot begin work assigned to another employee");
            orderRow = page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair"));
            orderRow.getByRole(AriaRole.LINK).click();
            page.waitForURL("**/work-orders/*/manage");
            page.getByLabel("Assignee").selectOption("tlovejoy");
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Assign")).click();
            page.waitForURL(baseUrl + "/");
            orderRow = page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair"));
            orderRow.getByText("ASSIGNED").waitFor(new Locator.WaitForOptions().setState(WaitForSelectorState.VISIBLE));
            page.getByTestId("logout-link").click();
            page.waitForURL("**/login");
            page.getByTestId("lovejoy-shortcut").click();
            page.waitForURL("**/");
            page.reload();
            page.waitForURL("**/");
            assertTrue(page.getByTestId("welcome-text").innerText().contains("tlovejoy"),
                "The server session should persist across hard navigation");
            orderRow = page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair"));
            page.getByLabel("Assigned to me").check();
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Search")).click();
            assertTrue(page.url().contains("assignedToMe=true"), "The assigned-to-me search should be reflected in the URL");
            assertTrue(page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair")).isVisible(),
                "Assigned-to-me should return the current employee's orders");
            page.getByLabel("Assigned to me").uncheck();
            page.getByLabel("Creator").selectOption("hsimpson");
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Search")).click();
            assertTrue(page.url().contains("creator=hsimpson"), "The creator search should be reflected in the URL");
            assertTrue(page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair")).isVisible(),
                "Creator filtering should match persisted creator identity");
            page.navigate(baseUrl + "/");
            orderRow = page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair"));
            orderRow.getByRole(AriaRole.LINK).click();
            page.waitForURL("**/work-orders/*/manage");
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Begin")).click();
            page.waitForURL(baseUrl + "/");
            orderRow = page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair"));
            orderRow.getByText("IN_PROGRESS").waitFor(new Locator.WaitForOptions().setState(WaitForSelectorState.VISIBLE));
            orderRow.getByRole(AriaRole.LINK).click();
            page.waitForURL("**/work-orders/*/manage");
            page.getByRole(AriaRole.BUTTON, new Page.GetByRoleOptions().setName("Complete")).click();
            page.waitForURL(baseUrl + "/");
            orderRow = page.locator("tbody tr").filter(new Locator.FilterOptions().setHasText("Acceptance repair"));
            orderRow.getByText("COMPLETE").waitFor(new Locator.WaitForOptions().setState(WaitForSelectorState.VISIBLE));
            context.close(); // flushes the recorded webm to disk
            capturedVideo = video.path();
            browser.close();
        }
        Files.copy(capturedVideo, videoDir.resolve("acceptance.webm"), StandardCopyOption.REPLACE_EXISTING);
        assertTrue(Files.size(videoDir.resolve("acceptance.webm")) > 0, "Browser video should be captured");
    }
}
