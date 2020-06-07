﻿module.exports = ['$scope', 'FileUploader', 'SessionService', 'DnnService', 'apiUrl',
    function ($scope, FileUploader, SessionService, DnnService, apiUrl) {

        // Store for errors.
        $scope.errors = [];

        // Should we be able to click continue?
        $scope.canContinue = function () {

            var canCon = true;

            // Have we uploaded at least one thing and is everything uploaded?
            if ($scope.uploader.getNotUploadedItems().length > 0
                || $scope.uploader.queue.length < 1) {

                // Can't continue.
                canCon = false;
            }

            return canCon;
        };

        // Wait for session guid.
        SessionService.sessionPromise.then(setupUploader);

        // Setup uploader.
        function setupUploader(session) {

            // Construct upload url.
            var uploadUrl = apiUrl + 'Session/AddPackage?guid=' + session.Guid;

            // Create uploader.
            var uploader = new FileUploader({
                url: uploadUrl,
                headers: DnnService.getSecurityHeaders()
            });

            // Place on scope.
            $scope.uploader = uploader;

            // Add .zip filter.
            uploader.filters.push({
                name: 'zipOnly',
                rejectionMessage: 'is not a .zip file.',
                fn: function (item, options) {

                    // Is there a name?
                    if (!item.name) {
                        return false;
                    }

                    // Very rudimentary check to see if there is a .zip extension.
                    var name = item.name;

                    // Get extension.
                    var ext = name.substring(name.lastIndexOf('.'));

                    // Is .zip?
                    if (ext.toLowerCase() !== '.zip') {
                        return false;
                    }

                    return true;
                }
            });

            // Add handling for failed file add.
            uploader.onWhenAddingFileFailed = function (item, filter, options) {
                $scope.errors.push(item.name + ' ' + filter.rejectionMessage);
            };
        }

    }];
