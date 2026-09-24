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
class WorkOrderAttachmentApiTest {
    @Autowired MockMvc mvc;
    @Autowired WorkOrderRepository workOrders;

    @BeforeEach void clear() { workOrders.deleteAll(); }

    private MockHttpSession loginAs(String username) throws Exception {
        var csrfResult = mvc.perform(get("/api/csrf")).andExpect(status().isOk()).andReturn();
        MockHttpSession session = (MockHttpSession) csrfResult.getRequest().getSession(false);
        String token = new com.fasterxml.jackson.databind.ObjectMapper().readTree(
            csrfResult.getResponse().getContentAsString()).get("token").asText();
        mvc.perform(post("/api/session").session(session).header(CsrfProtectionFilter.HEADER_NAME, token)
                .contentType(MediaType.APPLICATION_JSON).content("{\"username\":\"" + username + "\"}"))
            .andExpect(status().isOk());
        return session;
    }

    private static MockHttpServletRequestBuilder withCsrf(MockHttpSession session,
                                                          MockHttpServletRequestBuilder request) {
        return request.session(session).header(CsrfProtectionFilter.HEADER_NAME,
            session.getAttribute(CsrfProtectionFilter.SESSION_ATTRIBUTE));
    }

    @Test void creatorAddsAndListsAttachmentMetadataWithUploaderAndUtcDate() throws Exception {
        MockHttpSession creator = loginAs("hsimpson");
        String order = mvc.perform(withCsrf(creator, post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Repair pew\"}")))
            .andExpect(status().isCreated()).andReturn().getResponse().getContentAsString();
        String number = new com.fasterxml.jackson.databind.ObjectMapper().readTree(order).get("number").asText();

        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/attachments", number)
                .contentType(MediaType.APPLICATION_JSON)
                .content("{\"fileName\":\"damage-photo.jpg\",\"contentType\":\"image/jpeg\",\"fileSize\":2048}")))
            .andExpect(status().isCreated())
            .andExpect(jsonPath("$.workOrderId").isNotEmpty())
            .andExpect(jsonPath("$.fileName").value("damage-photo.jpg"))
            .andExpect(jsonPath("$.contentType").value("image/jpeg"))
            .andExpect(jsonPath("$.fileSize").value(2048))
            .andExpect(jsonPath("$.uploadedByName").value("Homer Simpson"))
            .andExpect(jsonPath("$.uploadedDate").value(org.hamcrest.Matchers.endsWith("Z")));

        mvc.perform(get("/api/work-orders/{number}/attachments", number).session(creator))
            .andExpect(status().isOk()).andExpect(jsonPath("$[0].fileName").value("damage-photo.jpg"));
        mvc.perform(get("/api/work-orders/{number}/attachments", number))
            .andExpect(status().isUnauthorized());
        mvc.perform(get("/api/work-orders/{number}/attachments", number).session(loginAs("msimpson")))
            .andExpect(status().isForbidden());
    }

    @Test void rejectsBlankOrOversizedMetadataFields() throws Exception {
        MockHttpSession creator = loginAs("hsimpson");
        String order = mvc.perform(withCsrf(creator, post("/api/work-orders").contentType(MediaType.APPLICATION_JSON)
                .content("{\"title\":\"Metadata checks\"}")))
            .andExpect(status().isCreated()).andReturn().getResponse().getContentAsString();
        String number = new com.fasterxml.jackson.databind.ObjectMapper().readTree(order).get("number").asText();
        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/attachments", number)
                .contentType(MediaType.APPLICATION_JSON).content("{\"fileName\":\" \" ,\"contentType\":\"text/plain\",\"fileSize\":1}")))
            .andExpect(status().isBadRequest());
        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/attachments", number)
                .contentType(MediaType.APPLICATION_JSON).content("{\"fileName\":\"image.jpg\",\"contentType\":\" \" ,\"fileSize\":1}")))
            .andExpect(status().isBadRequest());
        mvc.perform(withCsrf(creator, post("/api/work-orders/{number}/attachments", number)
                .contentType(MediaType.APPLICATION_JSON).content("{\"fileName\":\"image.jpg\",\"contentType\":\"image/jpeg\",\"fileSize\":-1}")))
            .andExpect(status().isBadRequest());
    }
}
