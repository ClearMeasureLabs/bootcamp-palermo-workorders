from django.contrib import admin
from django.urls import include, path
from workorders.views import api_operations

urlpatterns = [path("admin/", admin.site.urls), path("api/", api_operations, {"path": ""}), path("api/<path:path>", api_operations), path("", include("workorders.urls"))]
