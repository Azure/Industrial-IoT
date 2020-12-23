// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

function bulkUpdate(changes) {

    //
    // This script called as stored procedure to add, update, or delete all fed
    // changes in one batch. The script sets response body to the number of docs
    // processed so that it can be resumed until total number of docs are done.
    //
    var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();

    // The count of processed changes, also used as current doc index.
    var count = 0;
    // Validate input.
    if (!changes) {
        throw new Error("The array of changes is undefined or null.");
    }

    // If nothing to do, return now
    var totalChanges = changes.length;
    if (totalChanges === 0) {
        getContext().getResponse().setBody(0);
        return;
    }

    // Start
    tryProcess(changes[count], callback);

    //
    // Processes each doc in the bulk request - delete marks for deletion and cleanup
    //
    function tryProcess(change, cb) {

        var doc = change.doc;
        if (!doc) {
            throw new Error("Change Document must be defined and not null.");
        }

        if (change.delete) {
            // If delete, add soft delete and have compute clean it up in 5 minutes.
            doc._isDeleted = true;
            doc.ttl = 300;
        }
        else {
            // If not delete ensure that deleted flag is removed and no ttl set.
            delete doc._isDeleted;
            delete doc.ttl;
        }

        var isAccepted = false;
        if (doc._self) {
            // Use self link identifer and replace.
            isAccepted = collection.replaceDocument(doc._self, doc, {}, cb);
        }
        else {
            // Otherwise use upsert which would update or insert based on id
            isAccepted = collection.upsertDocument(collectionLink, doc, {}, cb);
        }

        if (!isAccepted) {
            getContext().getResponse().setBody(count);
        }
    }

    //
    // This is called when a document was processed.
    //
    function callback(err, doc, options) {

        if (err) {
            throw err;
        }

        count++;
        if (count >= totalChanges) {
            // If we have created all documents, we are done. Just set the response.
            getContext().getResponse().setBody(count);
        }
        else {
            // Process next document.
            tryProcess(changes[count], callback);
        }
    }
}
