package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.persistence.WorkOrderRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.mock.web.MockHttpSession;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.request.MockHttpServletRequestBuilder;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest @AutoConfigureMockMvc
class WorkOrderApiTest {
    @Autowired MockMvc mvc;
    @Autowired WorkOrderRepository repository;
    @BeforeEach void clear() { repository.deleteAll(); }

    private MockHttpSession loginAs(String username) throws Exception {
        var csrfResult = mvc.perform(get("/api/csrf")).andExpect(status().isOk()).andReturn();
        MockHttpSession session = (MockHttpSession) csrfResult.getRequest().getSession(false);
        String csrf = new com.fasterxml.jackson.databind.ObjectMapper().readTree(
            csrfResult.getResponse().getContentAsString()).get("token").asText();
        mvc.perform(post("/api/session").session(session).header(CsrfProtectionFilter.HEADER_NAME, csrf)
                .contentType(MediaType.APPLICATION_JSON).content("{\"username\":\"" + username + "\"}"))
            .andExpect(status().isOk()).andReturn();
        return session;
    }

    private static MockHttpServletRequestBuilder withCsrf(MockHttpSession session,
                                                          MockHttpServletRequestBuilder request) {
        return request.session(session).header(CsrfProtectionFilter.HEADER_NAME,
            session.getAttribute(CsrfProtectionFilter.SESSION_ATTRIBUTE));
    }

    @Test void createsListsAndTransitionsAWorkOrder() throws Exception {
        MockHttpSession creator = loginAs("hsimpson");
        String json = """
            {"title":"Repair sink","description":"Leaking","instructions":"Bring a ladder","roomNumber":"204","creatorName":"client-supplied identity","dueDate":"2026-10-01"}
            """;
        String response = mvc.perform(withCsrf(creator, post("/api/work-orders").contentType(MediaType.APPLICATION_JSON).content(json)))
            .andExpect(status().isCreated()).andExpect(jsonPath("$.status").value("DRAFT"))
            .andExpect(jsonPath("$.instructions").value("Bring a ladder"))
            .andExpect(jsonPath("$.creatorUsername").value("hsimpson"))
            .andExpect(jsonPath("$.creatorName").value("Homer Simpson"))
            .andExpect(jsonPath("$.version").value(0))
            .andExpect(jsonPath("$.urgency").value("NONE")).andReturn().getResponse().getContentAsString();
        String number = new com.fasterxml.jackson.databind.ObjectMapper().readTree(response).get("number").asText();
        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/assign", number).contentType(MediaType.APPLICATION_JSON)
                .content("{\"assignee\":\"Lee\"}")))
            .andExpect(status().isBadRequest());
        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/assign", number).contentType(MediaType.APPLICATION_JSON)
                .content("{\"assignee\":\"tlovejoy\"}")))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("ASSIGNED"))
            .andExpect(jsonPath("$.version").value(1));
        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/begin", number))).andExpect(status().isForbidden());
        MockHttpSession assignee = loginAs("tlovejoy");
        mvc.perform(withCsrf(assignee, post("/api/work-orders/{number}/begin", number)))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("IN_PROGRESS"));
        mvc.perform(withCsrf(assignee, post("/api/work-orders/{number}/shelve", number)))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("ASSIGNED"));
        mvc.perform(withCsrf(assignee, post("/api/work-orders/{number}/begin", number)))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("IN_PROGRESS"));
        mvc.perform(withCsrf(assignee, post("/api/work-orders/{number}/complete", number)))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("COMPLETE"));
        mvc.perform(get("/api/work-orders").session(creator)).andExpect(status().isOk()).andExpect(jsonPath("$[0].title").value("Repair sink"));
        mvc.perform(get("/api/work-orders")).andExpect(status().isUnauthorized());
    }

    @Test void rejectsBlankTitle() throws Exception {
        MockHttpSession session = loginAs("hsimpson");
        mvc.perform(withCsrf(session, post("/api/work-orders")
                .contentType(MediaType.APPLICATION_JSON).content("{\"title\":\" \"}")))
            .andExpect(status().isBadRequest());
    }

    @Test void rejectsMissingCsrfTokenAndProtectsHtmlCreateLengthLimits() throws Exception {
        MockHttpSession session = loginAs("hsimpson");
        mvc.perform(post("/api/work-orders").session(session).contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"No token\"}"))
            .andExpect(status().isForbidden());
        mvc.perform(withCsrf(session, post("/work-orders").param("title", "Valid title")
                .param("description", "x".repeat(4001))))
            .andExpect(status().isBadRequest());
        mvc.perform(withCsrf(session, post("/work-orders").param("title", "T".repeat(300))))
            .andExpect(status().is3xxRedirection());
        mvc.perform(withCsrf(session, post("/work-orders").param("title", "T".repeat(301))))
            .andExpect(status().isBadRequest());
        mvc.perform(get("/")).andExpect(status().is3xxRedirection()).andExpect(redirectedUrl("/login"));
    }

    @Test void validatesDemoLoginAndClearsServerSessionOnLogout() throws Exception {
        mvc.perform(get("/api/session")).andExpect(status().isUnauthorized());
        var csrfResult = mvc.perform(get("/api/csrf")).andExpect(status().isOk()).andReturn();
        MockHttpSession loginSession = (MockHttpSession) csrfResult.getRequest().getSession(false);
        String csrf = new com.fasterxml.jackson.databind.ObjectMapper().readTree(csrfResult.getResponse().getContentAsString()).get("token").asText();
        mvc.perform(post("/api/session").session(loginSession).header(CsrfProtectionFilter.HEADER_NAME, csrf).contentType(MediaType.APPLICATION_JSON)
                .content("{\"username\":\"not-an-employee\"}"))
            .andExpect(status().isBadRequest());
        MockHttpSession session = loginAs("tlovejoy");
        mvc.perform(get("/api/session").session(session)).andExpect(status().isOk())
            .andExpect(jsonPath("$.username").value("tlovejoy"))
            .andExpect(jsonPath("$.canCreateWorkOrder").value(true))
            .andExpect(jsonPath("$.canFulfillWorkOrder").value(true));
        mvc.perform(withCsrf(session, delete("/api/session"))).andExpect(status().isNoContent());
        mvc.perform(get("/api/session")).andExpect(status().isUnauthorized());
    }

    @Test void enforcesRoleCapabilitiesForCreateAndAssignment() throws Exception {
        MockHttpSession parishioner = loginAs("msimpson");
        mvc.perform(withCsrf(parishioner, post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Should be blocked\"}")))
            .andExpect(status().isForbidden());
        MockHttpSession creator = loginAs("hsimpson");
        String created = mvc.perform(withCsrf(creator, post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Role check\"}")))
            .andExpect(status().isCreated()).andReturn().getResponse().getContentAsString();
        String number = new com.fasterxml.jackson.databind.ObjectMapper().readTree(created).get("number").asText();
        MockHttpSession otherEmployee = loginAs("tlovejoy");
        mvc.perform(withCsrf(otherEmployee, post("/api/work-orders/{number}/assign", number)
                .contentType(MediaType.APPLICATION_JSON).content("{\"assignee\":\"hsimpson\"}")))
            .andExpect(status().isForbidden());
        mvc.perform(withCsrf(otherEmployee, post("/api/work-orders/{number}/cancel", number)))
            .andExpect(status().isForbidden());
        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/assign", number)
                .contentType(MediaType.APPLICATION_JSON).content("{\"assignee\":\"msimpson\"}")))
            .andExpect(status().isBadRequest());
    }

    @Test void overdueFilterExcludesClosedOrdersAndFutureOrders() throws Exception {
        MockHttpSession creator = loginAs("hsimpson");
        String oldDueOrder = mvc.perform(withCsrf(creator, post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Old open\",\"dueDate\":\"2000-01-01\"}")))
            .andExpect(status().isCreated()).andReturn().getResponse().getContentAsString();
        String openNumber = new com.fasterxml.jackson.databind.ObjectMapper().readTree(oldDueOrder).get("number").asText();
        String closedDueOrder = mvc.perform(withCsrf(creator, post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Old closed\",\"dueDate\":\"2000-01-01\"}")))
            .andReturn().getResponse().getContentAsString();
        String closedNumber = new com.fasterxml.jackson.databind.ObjectMapper().readTree(closedDueOrder).get("number").asText();
        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/cancel", closedNumber))).andExpect(status().isOk());
        mvc.perform(withCsrf(creator, post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Future open\",\"dueDate\":\"2099-01-01\"}")))
            .andExpect(status().isCreated());

        mvc.perform(get("/api/work-orders?overdueOnly=true").session(creator))
            .andExpect(status().isOk()).andExpect(jsonPath("$..number").value(org.hamcrest.Matchers.contains(openNumber)))
            .andExpect(jsonPath("$[0].urgency").value("OVERDUE"));
    }
}
