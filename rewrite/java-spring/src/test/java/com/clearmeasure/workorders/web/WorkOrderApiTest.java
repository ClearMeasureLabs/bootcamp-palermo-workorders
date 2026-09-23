package com.clearmeasure.workorders.web;

import com.clearmeasure.workorders.persistence.WorkOrderRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest @AutoConfigureMockMvc
class WorkOrderApiTest {
    @Autowired MockMvc mvc;
    @Autowired WorkOrderRepository repository;
    @BeforeEach void clear() { repository.deleteAll(); }

    @Test void createsListsAndTransitionsAWorkOrder() throws Exception {
        String json = """
            {"title":"Repair sink","description":"Leaking","roomNumber":"204","creatorName":"Mara","dueDate":"2026-10-01"}
            """;
        String response = mvc.perform(post("/api/work-orders").contentType(MediaType.APPLICATION_JSON).content(json))
            .andExpect(status().isCreated()).andExpect(jsonPath("$.status").value("DRAFT")).andReturn().getResponse().getContentAsString();
        String number = new com.fasterxml.jackson.databind.ObjectMapper().readTree(response).get("number").asText();
        mvc.perform(post("/api/work-orders/{number}/assign", number).contentType(MediaType.APPLICATION_JSON).content("{\"assignee\":\"Lee\"}"))
            .andExpect(status().isOk()).andExpect(jsonPath("$.status").value("ASSIGNED"));
        mvc.perform(post("/api/work-orders/{number}/begin", number)).andExpect(status().isOk()).andExpect(jsonPath("$.status").value("IN_PROGRESS"));
        mvc.perform(post("/api/work-orders/{number}/complete", number)).andExpect(status().isOk()).andExpect(jsonPath("$.status").value("COMPLETE"));
        mvc.perform(get("/api/work-orders")).andExpect(status().isOk()).andExpect(jsonPath("$[0].title").value("Repair sink"));
    }

    @Test void rejectsBlankTitle() throws Exception {
        mvc.perform(post("/api/work-orders").contentType(MediaType.APPLICATION_JSON).content("{\"title\":\" \"}"))
            .andExpect(status().isBadRequest());
    }
}
