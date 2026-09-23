from django.urls import path
from . import views
urlpatterns = [path("", views.work_order_list, name="work_order_list"), path("work-orders/new/", views.work_order_create, name="work_order_create"), path("work-orders/<int:pk>/", views.work_order_detail, name="work_order_detail"), path("work-orders/<int:pk>/status/", views.work_order_transition, name="work_order_transition"), path("health/", views.health, name="health")]
