from django.urls import path
from . import views

urlpatterns = [
    path("", views.work_order_list, name="work_order_list"),
    path("workorder/search", views.work_order_list, name="work_order_search"),
    path("login", views.login, name="login"),
    path("login/", views.login, name="login_legacy"),
    path("logout/", views.logout, name="logout"),
    path("workorder/manage", views.work_order_create, name="work_order_create"),
    path("work-orders/new/", views.work_order_create, name="work_order_create_legacy"),
    path("workorder/manage/<int:pk>", views.work_order_detail, name="work_order_detail"),
    path("work-orders/<int:pk>/", views.work_order_detail, name="work_order_detail_legacy"),
    path("work-orders/<int:pk>/status/", views.work_order_transition, name="work_order_transition"),
    path("work-orders/<int:pk>/attachments/", views.work_order_attachment_create, name="work_order_attachment_create"),
    path("health/", views.health, name="health"),
]
