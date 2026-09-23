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
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest @AutoConfigureMockMvc
class WorkOrderApiTest {
    @Autowired MockMvc mvc;
    @Autowired WorkOrderRepository repository;
    @BeforeEach void clear() { repository.deleteAll(); }

    private MockHttpSession loginAs(String username) throws Exception {
        var result = mvc.perform(post("/api/session").contentType(MediaType.APPLICATION_JSON)
                .content("{\"username\":\"" + username + "\"}"))
            .andExpect(status().isOk()).andReturn();
        return (MockHttpSession) result.getRequest().getSession(false);
    }

    @Test void createsListsAndTransitionsAWorkOrder() throws Exception {
        MockHttpSession creator = loginAs("hsimpson");
        String json = """
            {"title":"Repair sink","description":"Leaking","instructions":"Bring a ladder","roomNumber":"204","creatorName":"client-supplied identity","dueDate":"2026-10-01"}
            """;
        String response = mvc.perform(post("/api/work-orders").contentType(MediaType.APPLICATION_JSON).content(json).session(creator))
            .andExpect(status().isCreated()).andExpect(jsonPath("$.status").value("DRAFT"))
            .andExpect(jsonPath("$.instructions").value("Bring a ladder"))
            .andExpect(jsonPath("$.creatorUsername").value("hsimpson"))
            .andExpect(jsonPath("$.creatorName").value("Homer Simpson"))
            .andExpect(jsonPath("$.urgency").value("NONE")).andReturn().getResponse().getContentAsString();
        String number = new com.fasterxml.jackson.databind.ObjectMapper().readTree(response).get("number").asText();
        mvc.perform(post("/api/work-orders/{number}/assign", number).contentType(MediaType.APPLICATION_JSON)
                .content("{\"assignee\":\"Lee\"}").session(creator))
            .andExpect(status().isBadRequest());
        mvc.perform(post("/api/work-orders/{number}/assign", number).contentType(MediaType.APPLICATION_JSON)
                .content("{\"assignee\":\"tlovejoy\"}").session(creator))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("ASSIGNED"));
        mvc.perform(post("/api/work-orders/{number}/begin", number).session(creator)).andExpect(status().isForbidden());
        MockHttpSession assignee = loginAs("tlovejoy");
        mvc.perform(post("/api/work-orders/{number}/begin", number).session(assignee))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("IN_PROGRESS"));
        mvc.perform(post("/api/work-orders/{number}/shelve", number).session(assignee))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("ASSIGNED"));
        mvc.perform(post("/api/work-orders/{number}/begin", number).session(assignee))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("IN_PROGRESS"));
        mvc.perform(post("/api/work-orders/{number}/complete", number).session(assignee))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("COMPLETE"));
        mvc.perform(get("/api/work-orders")).andExpect(status().isOk()).andExpect(jsonPath("$[0].title").value("Repair sink"));
    }

    @Test void rejectsBlankTitle() throws Exception {
        mvc.perform(post("/api/work-orders").session(loginAs("hsimpson"))
                .contentType(MediaType.APPLICATION_JSON).content("{\"title\":\" \"}"))
            .andExpect(status().isBadRequest());
    }

    @Test void validatesDemoLoginAndClearsServerSessionOnLogout() throws Exception {
        mvc.perform(get("/api/session")).andExpect(status().isUnauthorized());
        mvc.perform(post("/api/session").contentType(MediaType.APPLICATION_JSON)
                .content("{\"username\":\"not-an-employee\"}"))
            .andExpect(status().isBadRequest());
        MockHttpSession session = loginAs("tlovejoy");
        mvc.perform(get("/api/session").session(session)).andExpect(status().isOk())
            .andExpect(jsonPath("$.username").value("tlovejoy"))
            .andExpect(jsonPath("$.canCreateWorkOrder").value(true))
            .andExpect(jsonPath("$.canFulfillWorkOrder").value(true));
        mvc.perform(delete("/api/session").session(session)).andExpect(status().isNoContent());
        mvc.perform(get("/api/session")).andExpect(status().isUnauthorized());
    }

    @Test void enforcesRoleCapabilitiesForCreateAndAssignment() throws Exception {
        MockHttpSession parishioner = loginAs("msimpson");
        mvc.perform(post("/api/work-orders").session(parishioner).contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Should be blocked\"}"))
            .andExpect(status().isForbidden());
        MockHttpSession creator = loginAs("hsimpson");
        String created = mvc.perform(post("/api/work-orders").session(creator).contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Role check\"}"))
            .andExpect(status().isCreated()).andReturn().getResponse().getContentAsString();
        String number = new com.fasterxml.jackson.databind.ObjectMapper().readTree(created).get("number").asText();
        MockHttpSession otherEmployee = loginAs("tlovejoy");
        mvc.perform(post("/api/work-orders/{number}/assign", number).session(otherEmployee)
                .contentType(MediaType.APPLICATION_JSON).content("{\"assignee\":\"hsimpson\"}"))
            .andExpect(status().isForbidden());
        mvc.perform(post("/api/work-orders/{number}/cancel", number).session(otherEmployee))
            .andExpect(status().isForbidden());
        mvc.perform(post("/api/work-orders/{number}/assign", number).session(creator)
                .contentType(MediaType.APPLICATION_JSON).content("{\"assignee\":\"msimpson\"}"))
            .andExpect(status().isBadRequest());
    }

    @Test void overdueFilterExcludesClosedOrdersAndFutureOrders() throws Exception {
        String oldDueOrder = mvc.perform(post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .session(loginAs("hsimpson")).content("{\"title\":\"Old open\",\"dueDate\":\"2000-01-01\"}"))
            .andExpect(status().isCreated()).andReturn().getResponse().getContentAsString();
        String openNumber = new com.fasterxml.jackson.databind.ObjectMapper().readTree(oldDueOrder).get("number").asText();
        String closedDueOrder = mvc.perform(post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .session(loginAs("hsimpson")).content("{\"title\":\"Old closed\",\"dueDate\":\"2000-01-01\"}"))
            .andReturn().getResponse().getContentAsString();
        String closedNumber = new com.fasterxml.jackson.databind.ObjectMapper().readTree(closedDueOrder).get("number").asText();
        mvc.perform(post("/api/work-orders/{number}/cancel", closedNumber).session(loginAs("hsimpson"))).andExpect(status().isOk());
        mvc.perform(post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .session(loginAs("hsimpson")).content("{\"title\":\"Future open\",\"dueDate\":\"2099-01-01\"}"))
            .andExpect(status().isCreated());

        mvc.perform(get("/api/work-orders?overdueOnly=true"))
            .andExpect(status().isOk()).andExpect(jsonPath("$..number").value(org.hamcrest.Matchers.contains(openNumber)))
            .andExpect(jsonPath("$[0].urgency").value("OVERDUE"));
    }
}
